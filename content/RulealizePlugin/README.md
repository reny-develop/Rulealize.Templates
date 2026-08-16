# Rulealize.Plugin.Example

A vocabulary for [Rulealize](https://github.com/reny-develop/Rulealize).

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.Example` |
| Namespace | `yourns` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

`yourns.example`.

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

`rulealize plugins` lists what actually registered, read back off the loaded runtime. An
operation you wrote and did not register is missing from that list, which is the cheapest
way there is to find a forgotten `AddExpression`.

## Adding an operation

1. Copy `src/Rulealize.Plugin.Example/ExampleNode.cs` and write yours
2. Add a line to `Register` in `VocabPlugin.cs`
3. `dotnet build && dotnet rulealize plugins` -- your name is in the list
4. Call it from `ruleset/probe.json` and `dotnet rulealize play ruleset/probe.json`

**Then delete `ExampleNode.cs`.** It measures the length of a text, which has nothing to do
with your vocabulary; it is there to be copied, not kept.

## License

Apache-2.0.
