# Rulealize.Plugin.Example

| | |
| --- | --- |
| Identifier | `Rulealize.Plugin.Example` |
| Namespace | `yourns` |
| Version | `1.0.0` |
| Reserved prefix | none |
| Depends on | [the value model](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/value-model.md), and nothing else |
| Notation | [how a plugin specification is written](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/specification-notation.md) |

<!--
  One sentence on what this vocabulary is for.

  A specification says what one version of one vocabulary provides, normatively. It ships
  with the plugin rather than with the runtime so that it can change when this package
  releases and not before -- there has to be a commit to point at to say what 1.2 meant.
-->

## Nodes

| Node | Kind |
| --- | --- |
| `yourns.example` | expression |

---

## `yourns.example`

### Form

```jsonc
{ "op": "yourns.example", "of": <expression:Text> }
```

### How it evaluates

The number of characters in `of`. `Null` when `of` is `Null`.

### Example

```jsonc
{ "op": "yourns.example", "of": "tally" }   // 5
```

---

## Decided

<!--
  The half of a specification that is worth writing and is usually missing: what was left
  out, and why. An operation nobody asked for is not free -- it is a name spent, a thing to
  keep working, and a thing a reader has to skip. Record the ones you turned down and what
  would change your mind.

  - **No `yourns.something`.** Writable as X plus Y, and nothing has needed it.
-->
