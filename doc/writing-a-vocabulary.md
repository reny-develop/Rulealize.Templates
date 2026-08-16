# Writing a vocabulary

Everything before this is scaffolding, and a template writes it. This is the part that is
yours: turning an idea for an operation into a class the runtime will call.

You need nothing of Rulealize's design to start. Read section 1, write an operation, and
come back for the rest when something asks for it.

```sh
dotnet new rulealize-plugin -n Rulealize.Plugin.Text
cd Rulealize.Plugin.Text
dotnet tool restore && dotnet build && dotnet rulealize plugins
```

---

## 1. The operation you already have

`ExampleNode.cs` is one operation, and every operation has this shape.

```csharp
internal sealed class ExampleNode(ExpressionNode operand) : ExpressionNode
{
    public static ExpressionNode Build(INodeBuildContext context) =>
        new ExampleNode(context.RequireExpression("of"));

    public override RuleValue Evaluate(IEvaluationContext context)
    {
        RuleValue value = operand.Evaluate(context);
        if (value.IsNull)
        {
            return RuleValue.Null;
        }

        return RuleValue.Number(value.AsText("text.example.of").Length);
    }
}
```

It answers to this:

```jsonc
{ "op": "text.example", "of": "tally" }   // 5
```

**Two methods, and they run at different times.**

| | When | What it sees |
| --- | --- | --- |
| `Build` | once, when the rule set is compiled | the JSON. The only place it appears |
| `Evaluate` | every time, and once per candidate during `GetValidInputs` | values. Never JSON |

That split is the whole architecture. `Build` turns a document into a tree of objects;
`Evaluate` walks the tree. Nothing re-reads the document, and nothing in `Evaluate` can ask
what the JSON said.

**`Evaluate` is always the same four steps.**

```
evaluate the children  ->  As* into ordinary C#  ->  compute  ->  back into a RuleValue
```

**Registering it is a separate act.** The class does not announce itself.

```csharp
registry.AddExpression("example", ExampleNode.Build);
```

The name is unqualified; the namespace comes from the manifest, so a vocabulary cannot
register into anybody else's. **This line is the one that gets forgotten**, and
`rulealize plugins` is how you find out — it lists what the loaded runtime says was
registered, not what your source says.

---

## 2. Writing your own

Copy the shape. Three things vary.

### How many arguments, and what they are called

```csharp
public static ExpressionNode Build(INodeBuildContext context) =>
    new AtNode(context.RequireExpression("text"), context.RequireExpression("index"));
```

The names are JSON property names and are yours to choose. What you ask for decides what a
rule set may write.

| Asking for | Gets you | Refusing it |
| --- | --- | --- |
| `RequireExpression("of")` | a child expression | missing is a build error |
| `OptionalExpression("separator")` | the same, or `null` | absent is legal |
| `RequireExpressionArray("of")` | `ImmutableArray<ExpressionNode>`, in order | order is part of your contract |
| `RequireString("as")` | a **literal** string from the document | writing an expression there is a build error |
| `RequireInt32("width")` / `OptionalInt32("min", 0)` | a literal number | |
| `OptionalBoolean("nullable", false)` | a literal boolean | |

The last three are the distinction worth understanding: **a static key is read at build
time and cannot be computed.** A board's width, the members of an enumeration, the name a
sequence binds its element to — these are facts about the document, not about a position,
and asking for them as literals is what lets them be checked once.

### Refusing bad input

There are two kinds of failure and they belong to different phases.

```csharp
// build time -- decidable from the document alone
throw context.Error("min", $"{low} is greater than max {high}, so no value satisfies this.");

// evaluation time -- only the values could have told you
throw new RuleEvaluationException("text.at.index", $"{index} is not an index.");
```

Prefer the first. Everything decided at build time is decided once, before any position
exists, and is reported with the path in the document where it happened.

**The `origin` string is not decoration.** `"text.at.index"` — operation, then property — is
what a reader sees when a rule set of nine hundred lines faults. Every `As*` takes one:

```csharp
value.AsText("text.at.text")
value.AsInt32("text.at.index")
```

### Reading is lenient, writing is strict

This is a rule the whole project follows, and your operations should too.

```
seq.elementAt(past the end) -> null -> grid.at(null) -> null -> cmp.eq(null, "black") -> false
```

Reading past the end of something **returns null rather than faulting**, so that a rule set
does not have to measure before it reads. Writing past the end is an error, because
silently discarding a write has no meaning.

Null propagation follows from the same idea: **if an argument is null, return null**, and
check before you convert.

```csharp
RuleValue value = operand.Evaluate(context);
if (value.IsNull)
{
    return RuleValue.Null;   // AsText would have faulted
}
```

---

## 3. Values

The only channel between vocabularies. Your operation never sees another plugin's types;
it sees these.

```csharp
// making
RuleValue.Text("t")    RuleValue.Number(5)    RuleValue.Boolean(true)    RuleValue.Null
RuleValue.Sequence(() => Walk(x))             RuleValue.Record(fields)

// taking apart -- each takes the origin label
value.IsNull
value.AsText("op.prop")      value.AsInt32("op.prop")     value.AsNumber("op.prop")
value.AsBoolean("op.prop")   value.AsSequence("op.prop")

// for messages
RuleValue.Describe(value)    // the text "black" / the number 3 / a sequence
```

Seven kinds: `Null`, `Boolean`, `Number`, `Text`, `Sequence`, `Record`, `Opaque`. Two things
about them decide most of what you write.

**Equality is defined for you.** Different kinds are never equal, `1` and `"1"` are not
equal, null equals null. Never write your own comparison of two `RuleValue`s — call
`Equals`.

**Some kinds have a canonical text and some do not.** `Text`, `Number` and `Boolean` do;
`Sequence` and `Record` do not, and `Opaque` does only if its plugin says so. That decides
two unrelated things: what `branch.match` can match on, and what may be an input argument.
A value with no canonical text cannot survive the round trip through an input document —
which is why compound input arguments are tuples and not records.

**One trap, and the compiler catches it.** A sequence must survive being enumerated twice.

```csharp
RuleValue.Sequence(() => Walk(text))   // right -- the factory runs per enumeration
RuleValue.Sequence(Walk(text))         // does not compile
```

`Walk` is a `yield` method, so it returns `IEnumerable<RuleValue>`, and `RuleValue.Sequence`
takes either a `Func<IEnumerable<RuleValue>>` or an `IReadOnlyList<RuleValue>`. A single-use
iterator is not in the overload set, so the dangerous form is unwritable rather than merely
discouraged.

Why it matters: a rule may bind a sequence once and read it twice, and a one-shot iterator
would return nothing the second time — no exception, just a wrong answer.

---

## 4. Iterating, and costing something

If your operation loops, two obligations come with it.

```csharp
foreach (RuleValue element in source.Evaluate(context).AsSequence("text.join.source"))
{
    context.CancellationToken.ThrowIfCancellationRequested();
    ...
}
```

**Check the token.** `GetValidInputs` evaluates a guard once per candidate, and that is the
work a caller is most likely to abandon.

**Say whether you short-circuit.** `logic.and` stops at the first false, and that is a
contract rather than an implementation detail — it is the only way a rule author can put a
cheap test in front of an expensive one. `logic.xor` cannot stop early, and its
documentation says so. Whichever yours is, write it down.

**Be pure.** Same state, same bindings, same answer, no observable effect. The runtime
memoizes definition results across candidates and may skip a binding nobody reads. An
operation that reaches outside turns a combinatorial search into a combinatorial number of
queries.

Values that change belong in the state. **The current date is a field handed to an
operation, not something an operation goes and finds out.**

---

## 5. Beyond expressions

Three kinds of node exist. Most vocabularies are expressions only; reach for the others
when the shape demands it.

| | Base class | Method | Registered with | Where it may appear |
| --- | --- | --- | --- | --- |
| Expression | `ExpressionNode` | `Evaluate(context)` | `AddExpression` | guards, definitions, effect arguments, domains, terminal |
| Effect | `EffectNode` | `Apply(context, draft)` | `AddEffect` | an input's `effects` only |
| Schema | `SchemaNode` | four members | `AddSchema` | `state.schema` only |

### Effects write to a draft

```csharp
public override void Apply(IEvaluationContext context, IStateDraft draft)
{
    draft.Set(path, value.Evaluate(context));
}
```

**Read from `context`, write to `draft`.** Everything an effect evaluates sees the state as
it was when the input was applied, not as amended by earlier effects in the same array. That
is what lets an input be written in the order a person would describe it. When you do need
what an earlier effect wrote — two effects editing one board — read it back with
`draft.Get(path)`.

### Introducing a name

If your operation binds a name for a subexpression, the way `seq.where` does with `as`:

```csharp
ExpressionNode source = context.RequireExpression("source");   // built first, deliberately
using (context.Scope.BeginScope())
{
    LocalSlot element = context.Scope.Declare(context.RequireString("as"));
    ExpressionNode predicate = context.RequireExpression("predicate");
}

// at evaluation
IEvaluationContext inner = context.Bind(element, currentValue);
```

Build the source **before** opening the scope, or a sequence could be defined in terms of
its own elements. `Bind` returns a new context; contexts are immutable, which is what makes
it safe for a lazy sequence to capture one.

You declare the name and somebody else reads it — `@x` belongs to the binding vocabulary,
which yours has never heard of. The scope machinery lives in the abstraction so that two
plugins can share a binding without meeting.

### Schemas own their JSON

A schema node decides how the values it describes are written and read, which is the seam
that keeps the state vocabulary from knowing what a board is. Four members: `IsNullable`,
`Validate`, `ReadJson`, `WriteJson`, plus `Normalize` if a value needs settling on the way
in — a lazy sequence stored in a state would otherwise hold on to the snapshot it was built
from.

**Validation reports rather than throws.** `Validate` writes to a sink so that one pass can
report every violation; stopping at the first would make a malformed document take as many
runs to fix as it has mistakes.

---

## 6. What not to add

A vocabulary is worth reading in proportion to what it leaves out. Two habits keep it that
way.

**Ask whether existing vocabulary already says it.** `text.chars` returns a `Sequence`, and
that one decision means `seq.any`, `seq.count` and `seq.where` work on text from the day it
ships — no `text.contains`, no `text.indexOf`, no second set of nodes meaning the same thing
as the first. Look for the seam where your values can join something that already exists
before you build a parallel world.

**Write down what you turned down.** Every standard vocabulary's specification ends with a
`Decided` section: the operations that were considered and refused, and what would change
the answer. `seq.distinct` is implementable and wanted by nothing. `seq.orderBy` had two
things waiting on it and both dissolved. That section is what stops the same argument being
had twice, and it is the half of a specification most likely to be missing.

An operation is never free. It is a name spent, a thing to keep working across versions, and
a thing every reader has to skip.

---

## The loop

```sh
dotnet build                                 # your assembly lands in plugin/
dotnet rulealize plugins                     # did it load, and what registered
dotnet rulealize check ruleset/probe.json    # does the document still compile
dotnet rulealize play ruleset/probe.json     # run it
```

`plugins` answers the two questions a new vocabulary fails on: whether the class was found
at all — public, not nested, not abstract, a parameterless constructor — and whether the
operation you just wrote reached `Register`.

## Where the rest is

| | |
| --- | --- |
| [The value model](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/value-model.md) | the kinds, equality, null propagation, and what each kind of node may do |
| [The standard vocabulary](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md) | the twelve, what each provides, and the conventions for one you keep to yourself |
| A specification per plugin | reached from the table above. Read one next to your own |
| [The design record](https://github.com/reny-develop/Rulealize/blob/main/doc/README.md#the-design-record) | why the DSL is shaped this way. Not required reading, and the best evidence that it works |
