# Writing a vocabulary

Everything before this is scaffolding, and a template writes it. This is the part that is
yours: turning an idea for an operation into a class the runtime will call.

You need nothing of Rulealize's design to start. Read section 1, write an operation, read
section 7 for where it goes in a document, and come back for the rest when something asks
for it.

```sh
dotnet new rulealize-plugin -n Rulealize.Plugin.Example
cd Rulealize.Plugin.Example
dotnet tool restore && dotnet build && dotnet rulealize plugins
```

What comes out is three worked operations, one of each kind of node, and every example below
is one of them. The names are `example.*` because the project was called
`Rulealize.Plugin.Example`; yours will carry your own namespace.

| | Base class | Method | Registered with | Where it may appear |
| --- | --- | --- | --- | --- |
| Expression | `ExpressionNode` | `Evaluate(context)` | `AddExpression` | guards, definitions, effect arguments, domains, terminal |
| Effect | `EffectNode` | `Apply(context, draft)` | `AddEffect` | an input's `effects` only |
| Schema | `SchemaNode` | four members | `AddSchema` | `state.schema` only |

Most vocabularies are expressions and nothing else, which is why they come first. **The call
that registered an operation is what decides its kind** — not the class, and not the name.

---

## 1. The expression you already have

`TopNode.cs` is one operation, and every expression has this shape.

```csharp
internal sealed class TopNode(ExpressionNode pile) : ExpressionNode
{
    public static ExpressionNode Build(INodeBuildContext context) =>
        new TopNode(context.RequireExpression("of"));

    public override RuleValue Evaluate(IEvaluationContext context)
    {
        RuleValue value = pile.Evaluate(context);
        if (value.IsNull)
        {
            return RuleValue.Null;
        }

        RuleValue top = RuleValue.Null;
        foreach (RuleValue token in value.AsSequence("example.top.of"))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            top = token;
        }

        return top;
    }
}
```

It answers to this:

```jsonc
{ "op": "example.top", "of": "$pile" }   // "green", for a pile of ["red", "green"]
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
registry.AddExpression("top", TopNode.Build);
```

The name is unqualified; the namespace comes from the manifest, so a vocabulary cannot
register into anybody else's. **This line is the one that gets forgotten**, and
`rulealize plugins` is how you find out — it lists what the loaded runtime says was
registered, not what your source says.

That is enough to write one. To call it from a document, skip to
[section 7](#7-calling-it-from-a-rule-set); `ruleset/probe.json` is annotated with the kind
of node each slot takes, and `rulealize check` will tell you when you have it wrong.

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
throw context.Error("max", $"must not be negative, but is {limit}.");

// evaluation time -- only the values could have told you
throw new RuleEvaluationException("example.push.token", "There is nothing to push.");
```

Prefer the first. Everything decided at build time is decided once, before any position
exists, and is reported with the path in the document where it happened:

```
'ruleset/probe.json' does not compile against 'plugin':
  /state/schema/pile/max: must not be negative, but is -1.
```

**The `origin` string is not decoration.** `"example.top.of"` — operation, then property — is
what a reader sees when a rule set of nine hundred lines faults. Every `As*` takes one:

```csharp
value.AsSequence("example.top.of")
value.AsText("example.push.token")
```

### Reading is lenient, writing is strict

This is a rule the whole project follows, and your operations should too.

```
seq.elementAt(past the end) -> null -> grid.at(null) -> null -> cmp.eq(null, "black") -> false
```

Reading past the end of something **returns null rather than faulting**, so that a rule set
does not have to measure before it reads. `example.top` of an empty pile is null for that
reason. Writing past the end is an error, because silently discarding a write has no meaning
— which is why `example.push` faults on a null token rather than pushing nothing.

Null propagation follows from the same idea: **if an argument is null, return null**, and
check before you convert.

```csharp
RuleValue value = pile.Evaluate(context);
if (value.IsNull)
{
    return RuleValue.Null;   // AsSequence would have faulted
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
foreach (RuleValue token in source.Evaluate(context).AsSequence("example.top.of"))
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

## 5. Effects

`PushNode.cs` is the second of the three. An effect is the only kind of node that changes
anything, and it may appear in an input's `effects` array and nowhere else — using one as an
expression is a build error, so the two meanings never have to be told apart by reading.

```csharp
internal sealed class PushNode(StatePath path, ExpressionNode token) : EffectNode
{
    public static EffectNode Build(INodeBuildContext context)
    {
        string field = context.RequireString("path");
        if (!context.State.TryResolve(field, out StatePath? path))
        {
            throw context.Error("path", $"'{field}' is not a field of the state schema.");
        }

        return new PushNode(path, context.RequireExpression("token"));
    }

    public override void Apply(IEvaluationContext context, IStateDraft draft)
    {
        RuleValue pushed = token.Evaluate(context);
        if (pushed.IsNull)
        {
            throw new RuleEvaluationException("example.push.token", "There is nothing to push.");
        }

        ImmutableArray<RuleValue> pile = [.. draft.Get(path).AsSequence("example.push.path"), pushed];
        draft.Set(path, RuleValue.Sequence(pile));
    }
}
```

```jsonc
{ "op": "example.push", "path": "pile", "token": "red" }
```

`Build` is the same method it was for an expression, and it is where a path becomes a
`StatePath`. **A field name is a static key**, resolved against the schema once at build
time, so a rule set that names a field which does not exist fails before any position exists:

```
  /inputs/push/effects[0]/path: 'pyle' is not a field of the state schema.
```

`Apply` is where an effect differs, and everything about it follows from **two places to
look and one place to write**.

| | |
| --- | --- |
| `context` | the state as it was **when the input was applied**, never as amended by an earlier effect in the same array |
| `draft` | what this transition has written so far |
| `draft.Set` | the only way to change anything |

**Read from `context`, write to `draft`.** Snapshot semantics are what let an input be
written in the order a person would describe it. Othello's `place` puts the stone down and
then flips what it captured; under sequential semantics the flip would rescan a board that
already had the new stone on it, and the rule author would have to hoist the computation into
a `bind.let` to get the right answer.

**`draft.Get` is the exception, and it is deliberate.** An effect that accumulates onto a
value another effect already touched — two writes to one board, or two pushes onto one pile —
reads it back from the draft. That is a read-modify-write, and the line above is what makes
two `example.push` effects in one input both land instead of the second overwriting the first.

Writes to the same field overwrite one another and the last wins, so an effect that is not
read-modify-write does not have to care what ran before it.

---

## 6. Schemas

`PileSchemaNode.cs` is the third. A schema node says what one state field holds, appears in
`state.schema` and nowhere else, and **is never evaluated** — there is no `Evaluate` and no
`IEvaluationContext` anywhere in it, because a schema describes values rather than producing
them.

What it does own is **the JSON its values are written as**. That is the seam that keeps the
state vocabulary from knowing what a board is: `state` moves a value in and out of a field
while `grid.board` decides that a board is written as a sparse object keyed by coordinate.
Switching to a dense array is a change to one schema node and nothing else — and a compact
spelling of your own, a duration as `"PT5M"` or a position as `"d3"`, has nowhere else to go.

Four members, and one optional fifth.

| | |
| --- | --- |
| `IsNullable` | whether `Null` is a legal value for a field of this schema |
| `Validate(value, sink)` | check a value already in hand |
| `ReadJson(element, sink)` | a state document arriving |
| `WriteJson(writer, value)` | a state document leaving |
| `Normalize(value)` | settle what an effect wrote, before it is stored |

```csharp
public override bool IsNullable => false;

public override void Validate(RuleValue value, ISchemaValidationSink sink)
{
    if (value is not SequenceValue pile)
    {
        sink.Violation($"Expected a pile but got {RuleValue.Describe(value)}.");
        return;
    }

    int count = 0;
    foreach (RuleValue token in pile)
    {
        if (token is not TextValue)
        {
            sink.Violation($"[{count}]", $"Expected a token but got {RuleValue.Describe(token)}.");
        }

        count++;
    }

    if (maximum is int limit && count > limit)
    {
        sink.Violation($"Expected at most {limit} tokens but got {count}.");
    }
}
```

**Validation reports rather than throws.** `Validate` and `ReadJson` both write to a sink so
that one pass can name every violation; stopping at the first would make a malformed state
document take as many runs to fix as it has mistakes. The two-argument `Violation` says where
*inside* the value — an index, a coordinate — and the sink composes that with the path of the
field it was created for.

The same goes for reading. A malformed element is reported and a best-effort value returned,
not thrown, because the runtime checks the sink afterwards and one bad field should not hide
the next:

```csharp
public override RuleValue ReadJson(JsonElement element, ISchemaValidationSink sink)
{
    if (element.ValueKind != JsonValueKind.Array)
    {
        sink.Violation("Expected an array of tokens.");
        return RuleValue.EmptySequence;   // best effort, not a fault
    }
    ...
}
```

`WriteJson` has nothing to check — the value has already satisfied this schema — and its only
contract is that what it writes is what `ReadJson` will be handed back.

**`Normalize` is the one most schemas do not need, and one kind always does.** It runs once
per field when a transition commits, before the new state is anything anybody can see. A
sequence is the case: the value model lets one be lazy over the state it was built from, so
storing it as it is would leave each state holding a way to recompute itself from the state
before it, and the chain would grow with every move. Enumerating it here ends the chain at
one.

```csharp
public override RuleValue Normalize(RuleValue value)
{
    if (value is not SequenceValue pile)
    {
        return value;
    }

    ImmutableArray<RuleValue> settled = [.. pile];
    return RuleValue.Sequence(settled);
}
```

It is not a place to reject anything. A value that does not satisfy the schema is
`Validate`'s business, and returning something the schema disallows would only move the fault
somewhere harder to read.

### Introducing a name

Any of the three kinds may bind a name for a subexpression, the way `seq.where` does with
`as` and `state.update` does with the field it is rewriting:

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

---

## 7. Calling it from a rule set

Writing the C# is the hard half; the JSON turns out to be smaller than it looks. A rule set
has **five slots that take a node**, and each of them takes exactly one kind. That is the
whole of what you need to know to edit `ruleset/probe.json`.

```jsonc
{
  "state": {
    "schema": { "pile": { … } },          // SCHEMA nodes, one per field
    "initial": { "pile": [] }             // plain JSON, in the shape that schema reads
  },
  "inputs": {
    "push": {
      "params": { "token": { "domain": { … } } },   // EXPRESSION -> a sequence of candidates
      "when":    { … },                             // EXPRESSION -> a boolean
      "effects": [ { … } ]                          // EFFECT nodes, in order
    }
  },
  "terminal": {
    "when":   { … },                      // EXPRESSION -> a boolean
    "result": "full"                      // a label, not a node
  }
}
```

| Slot | Takes | Asks |
| --- | --- | --- |
| `state.schema.<field>` | a **schema** | what does this field hold |
| `inputs.<name>.params.<p>.domain` | an **expression** giving a sequence | what values may this parameter take |
| `inputs.<name>.when` | an **expression** giving a boolean | is this input legal here |
| `inputs.<name>.effects[]` | **effects** | what does it change |
| `terminal.when` | an **expression** giving a boolean | is this position final |

Put an operation in the wrong slot and it is refused by name, before anything runs:

```
  /inputs/push/effects[0]: 'example.top' is an expression and cannot appear where an
  effect is expected.
```

### Everywhere an expression goes, three things may be written

```jsonc
{ "op": "cmp.ne", "left": { "op": "example.top", "of": "$pile" }, "right": "@token" }
```

| | |
| --- | --- |
| `{ "op": … }` | an operation. Its other properties are whatever its `Build` asked for |
| `"red"`, `3`, `true` | a **literal**. A bare JSON scalar is an expression that evaluates to itself |
| `"$pile"`, `"@token"` | **sugar**: a one-character prefix a vocabulary reserved |

Sugar is the part that reads like magic and is not. `rulealize plugins` prints the character
next to the vocabulary that owns it, and there are only three:

| | | |
| --- | --- | --- |
| `$field` | a state field | `Rulealize.Plugin.State` |
| `@name` | a parameter, or a name an operation bound | `Rulealize.Plugin.Binding` |
| `#name` | one of the document's own `defs` | `Rulealize.Plugin.Definition` |

Each is shorthand for an operation you could have written out, and each needs its vocabulary
in `requires` — which is why `probe.json` names `Binding` although no `bind.*` appears in it.

### Your own operation is not a special case

```jsonc
{ "op": "example.top", "of": "$pile" }
```

`example` came from the manifest, `top` from `AddExpression("top", …)`, and `of` from
`RequireExpression("of")` in `Build`. **Rename an argument in `Build` and this is the line
that changes.** Nothing else knows the property names.

Two things follow from returning ordinary values rather than types of your own. The standard
vocabulary works on them the day you ship —

```jsonc
{ "op": "seq.count", "source": "$pile" }   // a pile is a Sequence, so seq.* already fits
```

— and yours works on theirs, so an operation that takes a sequence takes one from anywhere.
That is worth choosing for: a kind the value model already has buys a vocabulary's worth of
operations for nothing, and a `Record` or an `Opaque` of your own buys none.

### The edit loop

`rulealize check` reads the document and nothing else, needs no network, and takes about as
long as saving the file. Edit, check, repeat; run it before you wonder why `play` is behaving.

```
'ruleset/probe.json' does not compile against 'plugin':
  /inputs/push/effects[0]/path: 'pyle' is not a field of the state schema.
```

The path is a path into **your document**, so the first `/` names the slot and the rest walks
down to the property at fault. Nothing here reports a line number, because nothing here read
your file as text.

---

## The loop

Four commands, and each answers a different question. What they print is the point, so here
is all of it, from the project the template just wrote.

```sh
dotnet build
```

Your assembly lands in `plugin/`, which is the folder `rulealize` reads. Nothing else has to
be copied anywhere.

```sh
dotnet rulealize plugins
```

```
plugin
  1 assembly, 1 vocabulary

  Rulealize.Plugin.Example 1.0.0  (example)
      example.pile                 schema
      example.push                 effect
      example.top                  expression

3 operations in total.
```

**Did it load, and what registered.** Those are two questions and this answers both. The list
is read back off the loaded runtime, not off your source, so an operation you wrote and did
not register is simply absent from it — which is the cheapest way there is to find a
forgotten `AddExpression`. The kind in the right-hand column is the call that registered it,
so a node registered with the wrong one shows up here rather than as a puzzling refusal in a
document later.

One vocabulary is what a folder holds before `restore` has fetched the rest; afterwards this
lists all five, and the reserved-prefix characters `$` and `@` appear beside the two that own
them.

When the class itself did not load, the count says `0 vocabularies` and the command asks the
runtime why, since a folder sweep passes over what it cannot use in silence:

```
plugin
  1 assembly, 0 vocabularies

Nothing loaded, and the runtime does not say why: a folder sweep passes
over what it cannot use. What is in there:

  Rulealize.Plugin.Example.ExamplePlugin
      is not public. A sweep only takes public types.
```

The conditions are public, not nested, not abstract, and a parameterless constructor.

```sh
dotnet rulealize restore ruleset/probe.json
```

```
  Rulealize.Plugin.Example (already in plugin)
  Rulealize.Plugin.Binding 1.0.0
  Rulealize.Plugin.Comparison 1.0.0
  Rulealize.Plugin.Sequence 1.2.0
  Rulealize.Plugin.State 1.0.0
4 plugins -> plugin, 1 already there
'ruleset/probe.json' compiles against it.
```

Once per document, or after editing its `requires`. **The vocabulary you are writing is
credited rather than fetched** — there is nothing on the feed to fetch — and from that moment
it and the four from nuget.org are indistinguishable to everything downstream.

```sh
dotnet rulealize check ruleset/probe.json
```

```
'ruleset/probe.json' compiles against 'plugin' (5 vocabularies, 29 operations).
```

**The question asked after every edit.** It reaches no network, and everything decidable from
the document is decided here — unknown operations, missing keys, expressions where literals
belong, unbound locals, nodes used where their kind does not fit:

```
  /inputs/push/when/left: 'example.tpo' is not an operation any loaded plugin provides.
  Check the rule set's 'requires'.

  /inputs/push/effects[0]: 'example.top' is an expression and cannot appear where an
  effect is expected.
```

So does a build-time refusal of your own, which is why section 2 prefers them:

```
  /state/schema/pile/max: must not be negative, but is -1.
```

```sh
dotnet rulealize play ruleset/probe.json
```

```
probe@1.0.0 from the initial state
Choose by number. 'state' prints the position, 'q' stops.

    1. push(token: red)
    2. push(token: green)
    3. push(token: blue)
> 1

    1. push(token: green)
    2. push(token: blue)
> 2

    1. push(token: red)
    2. push(token: green)
> 1

terminal: full
```

**Everything that is left after `check` passes: the values.** The list is `GetValidInputs`,
so an operation of yours ran once per candidate to produce it; the transition ran your
effects; and the list shrinking from three to two is a guard of yours returning false. `state`
prints the position without spending a move, which is the only sight you get of a schema
node's `WriteJson`:

```
> state
{
  "$schema": "rulealize/state/v1",
  "ruleSet": "probe@1.0.0",
  "data": {
    "pile": [
      "red"
    ]
  }
}
```

A rule set with no way out of a position says so rather than looping:

```
No legal input, and the position is not terminal.
```

## One move at a time: `moves` and `apply`

`play` holds the position in memory and then forgets it. The other two put it in a file, which
is what you want the moment a bug takes three moves to reach: **the state is a document, so a
position worth returning to is a file worth keeping.**

**`rulealize moves` lists what is legal from a position** and changes nothing.

```
$ dotnet rulealize moves ruleset/probe.json
probe@1.0.0 from the initial state (ongoing)      <- standard error
push(token: red)
push(token: green)
push(token: blue)
3 legal inputs, 3 candidates evaluated            <- standard error
```

The moves go to standard output and everything about them to standard error, so
`moves … | wc -l` counts moves and nothing else. The last line is the one to read while
developing: **3 candidates evaluated** is the domain, and **3 legal** is how many survived
your guard. A guard that is wrong shows up as those two numbers disagreeing with what you
expected long before anything faults.

`GetValidInputs` stops at `--limit` rather than searching for as long as it takes, and it
always says when it did, because the worst failure this command has is a truncated answer
that looks complete.

**`rulealize apply` applies one and writes the state it reached.** The input is named exactly
the way `moves` printed it; nothing parses that text, so what can be named is what was
offered.

```
$ dotnet rulealize apply ruleset/probe.json "push(token: red)" > s1.json
push(token: red) applied to the initial state     <- standard error
```

The new state goes to standard output and the commentary to standard error, so redirecting
gives a state document and nothing else. Then `--state` starts from it:

```
$ dotnet rulealize moves ruleset/probe.json --state s1.json
probe@1.0.0 from 's1.json' (ongoing)
push(token: green)
push(token: blue)
2 legal inputs, 3 candidates evaluated
```

**Three candidates, two legal**: `push(token: red)` is gone because `example.top` now returns
`red` and the guard refuses a repeat. That one line is your operation being exercised.

`--write` amends the state file in place instead, which is one file name for a sequence of
moves rather than one per move:

```
$ dotnet rulealize apply ruleset/probe.json "push(token: green)" --state s1.json --write
push(token: green) applied to 's1.json'
's1.json' updated

$ dotnet rulealize apply ruleset/probe.json "push(token: blue)" --state s1.json --write
push(token: blue) applied to 's1.json' -- terminal (full)
's1.json' updated
```

The `-- terminal (full)` is `terminal.when` and `terminal.result` from the document. A
sequence of those lines is a regression test you can paste into a shell script.

**A refusal names what was on offer instead**, which is usually enough to see why:

```
$ dotnet rulealize apply ruleset/probe.json "push(token: red)" --state s1.json
'push(token: red)' is not legal from 's1.json'.
These are:
  push(token: green)
  push(token: blue)

To apply something the rule set refuses -- to check that it does -- write the
input document and pass --input <file>.
```

### Checking that a guard says no

That last line matters more than it looks. **Naming an input goes through the list of legal
ones**, so the named form can only ever apply something already offered — which means it
cannot ask the question a guard exists to answer. Half of what you write is a refusal, and
none of it is reachable this way.

An input document names the input directly and skips the list:

```jsonc
// repeat.json -- the same colour twice, which the guard must refuse
{
  "$schema": "rulealize/input/v1",
  "ruleSet": "probe@1.0.0",
  "input": "push",
  "args": { "token": "red" }
}
```

```
$ dotnet rulealize apply ruleset/probe.json --input repeat.json --state s1.json
refused from 's1.json': 'push' is not allowed in this state.
These are:
  push(token: green)
  push(token: blue)
```

**That refusal is a pass, not a failure.** It is the runtime running your guard against an
input nobody offered and getting `false`, which is the only direct evidence that the guard
does what it claims — and the exit code is non-zero, so a shell script can assert on it.

`moves --json` prints every legal input as exactly the `input` and `args` pair above, so the
quickest way to write one is to copy the nearest legal input and change the argument you want
refused. `ruleSet` is optional and worth keeping: when it is there it is checked, so an input
document left over from an earlier version of the document says so instead of applying.

| | |
| --- | --- |
| `--plugins <folder>` | where the vocabularies are. Default `plugin` |
| `--state <file>` | the position to start from. Default the rule set's own `state.initial` |
| `--input <file>` | `apply` an input document rather than one `moves` named. The only way to reach an input the rule set refuses |
| `--write` | amend `--state` in place instead of writing to standard output |
| `--limit <n>` | candidates `GetValidInputs` may try. Default 10000 |
| `--json` | `moves`, as the runtime writes them |

## Where the rest is

| | |
| --- | --- |
| [The value model](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/value-model.md) | the kinds, equality, null propagation, and what each kind of node may do |
| [The standard vocabulary](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md) | the twelve, what each provides, and the conventions for one you keep to yourself |
| [The command line](https://github.com/reny-develop/Rulealize.Cli) | every command above, and what it will not do |
| [Rule sets worth reading](https://github.com/reny-develop/Rulealize/blob/main/doc/README.md) | reversi, chess, shogi, a shift roster and a deployment pipeline, each written out in full |
| A specification per plugin | reached from the table above. Read one next to your own |
| [The design record](https://github.com/reny-develop/Rulealize/blob/main/doc/README.md#the-design-record) | why the DSL is shaped this way. Not required reading, and the best evidence that it works |
