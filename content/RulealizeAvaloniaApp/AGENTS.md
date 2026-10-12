# This application's folder, and the tools in it

This folder is an Avalonia application made of a rule set and XAML. What follows is what is in
it, how each file is read, and what each tool does. How an application like this is worked on —
what is the machine's and what stays with a person — is
[Rule-Derived Test Design](https://github.com/reny-develop/rule-derived-test-design)'s to say,
and is not repeated here.

## Who reads what

The folder is made for somebody who builds the application in VS Code with RulealizeStudio.Avalonia,
and who need not be an engineer. What they read is three things: each window, drawn as the window
it is; each specification, drawn as a diagram, sentences and a table; and **Test cases**, every
situation the rules allow shown as the application's own windows, with what a change did shown as
the windows before it and after it. They do not read any file here as text. The rule sets, the test
designs, the label documents, Rulealize, Ruledger, XAML and JSON are not words of theirs: what
there is to say to them about the application is said in the words of its windows and its
specifications.

They place the controls on a window in RulealizeStudio's screen editor, which writes where each
is the way Visual Studio's WPF designer does: in a `Grid`, aligned to the top left of its cell with
its distance from there as its `Margin`, and its size as its `Width` and `Height`.

## What is in the folder

| | |
| --- | --- |
| `MainWindow.axaml`, and any other `.axaml` | the design: each a window, bound by name to what the rules give |
| `specification.json`, and any other `<name>.specification.json` | what the application should do, as UML state machines, each element bound to the rules that carry it out |
| `<rule set>.json` | the rules; there may be more than one |
| `<rule set>.test-design.json` | everything the rules allow, as `ruledger derive` writes it down |
| `<rule set>.labels.<language>.json` | what the application says, in one language, where the rules refuse with a code |
| the `.csproj` | the vocabularies the rule set requires, as packages |

The design and the specifications together are the blueprint. There is no C# beyond `Program.cs`,
which is the same in every application, and is not edited when a window or a rule set is added; the
model each window binds to is generated from its rule set when the application is built. A rule set
is any JSON file here whose `$schema` says it is one, and the files beside it are named after it:
`booking.json`, `booking.test-design.json`, `booking.labels.en.json`. A specification is any JSON file
here whose `$schema` says it is one; it is written before there are rules, and is named after none.
How many of each there are is not counted against the others: two windows may bind one rule set, and
one specification may ask for rules of two.

## The windows

Every `.axaml` here whose root is a `Window` is a window of the application, and each binds the
model of one rule set, as its `x:DataType`; windows that bind the same rule set stand in the same
place. When a window is shown, and what its × does, are the rules' to decide, bound in its XAML as
anything else is — nothing in C# opens or closes a window:

```xml
<Window xmlns:rs="using:RulealizeStudio.Hosting"
        x:DataType="m:BookingModel"
        rs:RuleWindow.ShowWhen="{Binding Confirm.IsOffered}"
        rs:RuleWindow.CloseWith="{Binding Change.Apply}">
```

`rs:RuleWindow.ShowWhen` shows the window while what it is bound to is true — an input's
`IsOffered`, `IsTerminal`, a projection that is true or false — and hides it when it is not; a window
without it is shown from the start. `rs:RuleWindow.CloseWith` makes the window's × a move, as a
button's `Command` makes a press one, with `rs:RuleWindow.CloseParameter` as its `CommandParameter`;
the window goes only if that move makes its `ShowWhen` false. A window bound to a rule set without
`CloseWith` cannot be closed by its × at all, since what somebody can do on the screen is a move the
test design walks, or nothing. When no window is shown, the application ends.

## The specification

A specification is a UML state machine: the states the application is in, the transitions
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

An element may be bound to nothing yet. Where the application has more than one rule set, a binding
names the one it is to, by its file without `.json`, before a `#`: `booking#/inputs/setParty`.

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

**The names the design binds.** A window binds the names its rule set gives, as the
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

**What a refusal says.** A `validate` clause refuses with a code, and so does an open parameter's
`invalid`, for a value its schema does not admit. The sentence shown for either is in a label
document beside the rule set, keyed `/inputs/<input>/validate/<code>` — the name above, which an
`invalid` code has too; a code with no label is shown as the code:

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
| `dotnet rulealize-studio agree` | compares every specification with every rule set; reports a binding to a rule the rules lack, a rule no element of any specification asks for, and an element a specification refers to and does not have |
| `dotnet ruledger derive <rule set>.json --plugins bin/Debug/net10.0` | walks the rules and writes the test design, carrying the `edits` already in it; reports a choice it could not carry |
| `dotnet ruledger diff <rule set>.test-design.json <rule set>.json --plugins bin/Debug/net10.0` | holds the test design against the rules as they are now; reports where they decide differently, and a choice it could not carry |
| `dotnet rulealize-studio replay` | stands the application as last built in every state the test design names, presses the control for every move on whichever window shows it — a window's × among them — and tries every refused value; reports where the windows do not do what the design says, or do not say a refusal as its label document does |
| `dotnet rulealize-studio show` | lists the test design's situations; with `--state <name> --out <folder>`, writes pictures of every window shown, standing in one |

Where there is more than one rule set, `check`, `replay` and `show` are told which with
`--rules <rule set>.json`, and `ruledger` is given it as above.

`dotnet build`, and `rulealize-studio`'s `check`, `agree` and `replay`, write what they report the way a compiler
does: the file, the place, what is there. `rulealize-studio` and `ruledger` exit 0 when there is
nothing to report, 1 when it could not be done, 2 when the command line was not understood, and
3 when there is something to report.

Where a parameter is typed rather than picked from what the rules list — a name, say — the walk
follows only the values written in the design's `edits`, in the form Ruledger's
[an edit](https://github.com/reny-develop/Ruledger/blob/main/doc/test-design.md#an-edit) gives.
A value the rules refuse is written down as refused, with its code, and the choice is reported as
not carried.
