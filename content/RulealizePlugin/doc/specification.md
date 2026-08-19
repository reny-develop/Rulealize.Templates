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
| `yourns.pile` | schema |
| `yourns.push` | effect |
| `yourns.top` | expression |

---

## `yourns.pile`

A state field holding an ordered pile of tokens, written as an array of strings.

### Form

```jsonc
{ "op": "yourns.pile", "max": <integer literal, optional> }
```

`max` bounds the number of tokens. A negative `max` is a build error.

### The value

A `Sequence` of `Text`. Never `Null`; an empty pile is written `[]`.

### JSON

```jsonc
"pile": ["red", "green"]
```

---

## `yourns.push`

An effect. Adds one token to the end of a pile field.

### Form

```jsonc
{ "op": "yourns.push", "target": <expression:Sequence>, "token": <expression:Text> }
```

`target` is written `"$pile"`. It is resolved at build time: a target that does not denote a
state field, or denotes one whose schema is not `yourns.pile`, is a build error.

### How it applies

`token` is evaluated against the snapshot, the pile is read from the draft, and the token is
appended. Two `yourns.push` effects in one input both land, in the order written.

Evaluation faults when `token` is `Null`: writing is strict.

---

## `yourns.top`

### Form

```jsonc
{ "op": "yourns.top", "of": <expression:Sequence> }
```

### How it evaluates

The last token of `of`. `Null` when `of` is `Null`, and `Null` when the pile is empty:
reading is lenient.

### Example

```jsonc
{ "op": "yourns.top", "of": "$pile" }   // "green"
```

---

## Decided

<!--
  The half of a specification that is worth writing and is usually missing: what was left
  out, and why. Record the ones you turned down and what would change your mind.

  - **No `yourns.something`.** Writable as X plus Y, and nothing has needed it.
-->
