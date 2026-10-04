# This application's folder, and the tools in it

This folder is an Avalonia application made of a rule set and XAML. What follows is what is in
it, how each file is read, and what each tool does. How an application like this is worked on —
what is the machine's and what stays with a person — is
[Rule-Derived Test Design](https://github.com/reny-develop/rule-derived-test-design)'s to say,
and is not repeated here.

## Who reads what

The folder is made for somebody who builds the application in VS Code with RulealizeStudio.Avalonia,
and who need not be an engineer. What they read is three things: the screen, drawn as the window it
is; the specification, drawn as a diagram, sentences and a table; and **On the screen**, every
situation the rules allow shown as the application's own window, with what a change did shown as
the window before it and after it. They do not read any file here as text. The rule set, the test
design, the label documents, Rulealize, Ruledger, XAML and JSON are not words of theirs: what
there is to say to them about the application is said in the words of its screen and its
specification.

They place the controls on the window in RulealizeStudio's screen editor, which writes where each
is the way Visual Studio's WPF designer does: in a `Grid`, aligned to the top left of its cell with
its distance from there as its `Margin`, and its size as its `Width` and `Height`.

## What is in the folder

| | |
| --- | --- |
| `MainWindow.axaml` | the design: the window, bound by name to what the rules give |
| `specification.json` | what the application should do, as a UML state machine, each element bound to the rules that carry it out |
| `<rule set>.json` | the rules |
| `<rule set>.test-design.json` | everything the rules allow, as `ruledger derive` writes it down |
| `<rule set>.labels.<language>.json` | what the application says, in one language, where the rules refuse with a code |
| the `.csproj` | the vocabularies the rule set requires, as packages |

The design and the specification together are the blueprint. There is no C# beyond `Program.cs`,
which is the same in every application; the model the window binds to is generated from the rule
set when the application is built. The rule set is any JSON file here whose `$schema` says it is
one, and the files beside it are named after it: `booking.json`, `booking.test-design.json`,
`booking.labels.en.json`. The specification is written before there are rules, and is named after
none.

## The specification

`specification.json` is a UML state machine: the states the application is in, the transitions
between them — each what the person does, and what it waits for — and notes on either, or on the
whole, for what is said about them that is not a move. Each of the three is an element, under an
id of its own, and holds what it says and the rules that carry it out, each by a name the rule set
gives it — never a place in the text:

```json
{
  "$schema": "rulealize-studio/state-machine/v1",
  "initial": "named",
  "states": {
    "named": { "name": "Named", "says": "There is a name.", "rules": ["/state/schema/stage"] }
  },
  "transitions": {
    "party-size": {
      "from": "named", "to": "named", "name": "Set the party",
      "says": "A party is one to six people.",
      "rules": ["/state/schema/party", "/inputs/setParty", "/inputs/setParty/params/size"]
    }
  },
  "notes": {
    "party-unchanged": {
      "on": "party-size", "says": "Setting the party to the size it already is is refused.",
      "rules": ["/inputs/setParty/validate/party.unchanged"]
    }
  }
}
```

A state may be `"final": true`, and a transition may have a `guard`, in words. The Studio's editor
writes the file in one layout, whoever wrote it last; nothing else binds it to the rules.

| a rule | its name |
| --- | --- |
| a state field and what its schema allows | `/state/schema/<field>` |
| an input | `/inputs/<input>` |
| a parameter of it, its guard, who may make it, its effects | `/inputs/<input>/params/<parameter>`, `/inputs/<input>/when`, `/inputs/<input>/actor`, `/inputs/<input>/effects` |
| a `validate` clause, by its code | `/inputs/<input>/validate/<code>` |
| a definition, a projection, the ending | `/definitions/<name>`, `/projections/<name>`, `/terminal` |

An element may be bound to nothing yet.

## The rule set

How a rule set is written is Rulealize's to say:
[a rule set in five minutes](https://github.com/reny-develop/Rulealize/blob/main/doc/ruleset-quickguide.md),
then [writing a rule set](https://github.com/reny-develop/Rulealize/blob/main/doc/ruleset-guide.md)
for the whole language, and [the runtime's semantics](https://github.com/reny-develop/Rulealize/blob/main/doc/runtime.md)
for what it does with one.

**The vocabularies it loads.** The operations a rule set uses come from vocabularies, which its
`requires` names, with the versions that will do;
[vocabulary](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md) says where the
published ones are listed. The application loads each as a package: a `PackageReference` in the
`.csproj`'s last `ItemGroup`, at a version `requires` allows.

**The names the design binds.** `MainWindow.axaml` binds the names the rules give, as the
generated model spells them — each word capitalised and anything between words dropped, so the
input `setName` and one called `set-name` are both bound as SetName:

```xml
<TextBlock Text="{Binding State.Name}" />                    <!-- the state field name -->
<TextBox Text="{Binding SetName.To}"                          <!-- setName's open parameter to -->
         MaxLength="{Binding SetName.ToLimits.MaxLength}" />  <!-- and its bounds -->
<ComboBox ItemsSource="{Binding ChooseSeat.SeatOptions}" />   <!-- what seat may be -->
<Button Command="{Binding SetName.Apply}" />                  <!-- the input itself -->
<TextBlock Text="{Binding Booking.Who}" />                    <!-- who, in the projection booking -->
```

A name the design binds that the rules lack fails the build, and says which.

**What a refusal says.** A `validate` clause refuses with a code. The sentence shown for it is in
a label document beside the rule set, keyed by the clause's name above; a code with no
label is shown as the code:

```json
{
  "$schema": "rulealize-studio/labels/v1",
  "labels": { "/inputs/setParty/validate/party.unchanged": "The party is already that size." }
}
```

## The tools

`dotnet tool restore` fetches the two pinned in `.config/dotnet-tools.json`. Each command below is
run in this folder, and is the program the Rulealize extension runs to show the same thing.

| command | what it does, and what it reports |
| --- | --- |
| `dotnet build` | builds the application, the model generated from the rule set; a name the design binds that the rules lack is a build error |
| `dotnet rulealize-studio check` | compiles the rule set against the vocabularies the build put beside the application; reports where the runtime found a fault |
| `dotnet rulealize-studio agree` | compares the specification with the rules; reports a binding to a rule the rules lack, a rule no element asks for, and an element the specification refers to and does not have |
| `dotnet ruledger derive <rule set>.json --plugins bin/Debug/net10.0` | walks the rules and writes the test design, carrying the `edits` already in it; reports a choice it could not carry |
| `dotnet ruledger diff <rule set>.test-design.json <rule set>.json --plugins bin/Debug/net10.0` | holds the test design against the rules as they are now; reports where they decide differently, and a choice it could not carry |
| `dotnet rulealize-studio replay` | stands the application as last built in every state the test design names, presses the control for every move, and tries every refused value; reports where the screen does not do what the design says, or does not say a refusal as its label document does |
| `dotnet rulealize-studio show` | lists the test design's situations; with `--state <name> --out <folder>`, writes pictures of the window standing in one |

`dotnet build`, and `rulealize-studio`'s `check`, `agree` and `replay`, write what they report the way a compiler
does: the file, the place, what is there. `rulealize-studio` and `ruledger` exit 0 when there is
nothing to report, 1 when it could not be done, 2 when the command line was not understood, and
3 when there is something to report.

Where a parameter is typed rather than picked from what the rules list — a name, say — the walk
follows only the values written in the design's `edits`, in the form Ruledger's
[an edit](https://github.com/reny-develop/Ruledger/blob/main/doc/test-design.md#an-edit) gives.
A value the rules refuse is written down as refused, with its code, and the choice is reported as
not carried.
