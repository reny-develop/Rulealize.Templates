# Rulealize.Plugin.Example

A vocabulary for [Rulealize](https://github.com/reny-develop/Rulealize).

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.Example` |
| Namespace | `yourns` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

`yourns.pile`, `yourns.push`, `yourns.top`.

<!--
  Say here, in a paragraph, the one judgement this vocabulary is built around: what it
  refuses and why, or where its boundary with a neighbouring vocabulary is. That is what a
  reader wants from a landing page, and it is the thing only you can write.
-->

## Running it

```sh
dotnet tool restore                          # the rulealize command, pinned in .config
dotnet build                                 # drops this vocabulary into plugin/
dotnet rulealize plugins                     # what loaded, and what it registered
dotnet rulealize restore ruleset/probe.json  # fetch the standard vocabularies it also names
dotnet rulealize play ruleset/probe.json     # walk it
```

`rulealize plugins` lists what actually registered, read back off the loaded runtime:

```
plugin
  1 assembly, 1 vocabulary

  Rulealize.Plugin.Example 1.0.0  (yourns)
      yourns.pile                 schema
      yourns.push                 effect
      yourns.top                  expression

3 operations in total.
```

An operation you wrote and did not register is missing from that list, which is the cheapest
way there is to find a forgotten `AddExpression`.

## One move at a time

`play` holds the position in memory. To keep one, or to reach the same position twice while
chasing something, put it in a file:

```sh
dotnet rulealize moves ruleset/probe.json                          # what is legal here
dotnet rulealize apply ruleset/probe.json "push(token: red)" > s1.json
dotnet rulealize moves ruleset/probe.json --state s1.json          # what is legal now
dotnet rulealize apply ruleset/probe.json "push(token: blue)" --state s1.json --write
```

```
probe@1.0.0 from 's1.json' (ongoing)
push(token: green)
push(token: blue)
2 legal inputs, 3 candidates evaluated
```

An input is named exactly the way `moves` printed it. The last line is the one to watch while
developing: **candidates** is the size of the domain and **legal** is how many survived the
guard, so an operation that is quietly wrong shows up as those two numbers disagreeing with
what you expected.

Naming an input goes through that list, so it can only apply something already offered. To
check that a guard **refuses** what it should -- which is half of what a guard is for -- write
the input document and pass it directly:

```sh
dotnet rulealize apply ruleset/probe.json --input repeat.json --state s1.json
```

```
refused from 's1.json': 'push' is not allowed in this state.
These are:
  push(token: green)
  push(token: blue)
```

That refusal is the result you wanted, and the exit code is non-zero, so a script can assert
on it. `dotnet rulealize moves ruleset/probe.json --json` prints legal inputs in the shape
such a document takes.

## Adding an operation

1. Copy the example of the kind you want and write yours
2. Add a line to `Register` in `VocabPlugin.cs`
3. `dotnet build && dotnet rulealize plugins` -- your name is in the list
4. Call it from `ruleset/probe.json`, then `dotnet rulealize check ruleset/probe.json`
5. `dotnet rulealize play ruleset/probe.json` -- walk it

`ruleset/probe.json` labels every slot with the kind of node that may go in it, which is most
of what editing it takes. `check` reads the document and nothing else, so it is the fast half
of the loop.

## The example is meant to be deleted

Three files, one per kind of node, and together they are a pile of tokens that has nothing to
do with your vocabulary. They are there to be copied, not kept.

| | |
| --- | --- |
| `TopNode.cs` | an **expression**: produces a value. The kind almost everything is |
| `PushNode.cs` | an **effect**: writes to the state, and may appear only in an input's `effects` |
| `PileSchemaNode.cs` | a **schema**: says what a state field holds, and owns how it is written and read |

`ruleset/probe.json` calls all three, so it goes when they do.

[**Writing a vocabulary**](https://github.com/reny-develop/Rulealize.Templates/blob/main/doc/writing-a-vocabulary.md)
is the guide to what goes in those files: what `Build` and `Evaluate` are for, how to fail,
what a value is, and the one trap that has a compiler error waiting for it.

## Licence

None chosen. Add the terms you want, as a `LICENSE` file and a `PackageLicenseExpression` in
the csproj; nothing here picks one for you.
