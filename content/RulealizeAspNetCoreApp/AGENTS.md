# This application's folder, and the tools in it

This folder is an ASP.NET Core application made of rule sets and Razor screens. What follows is
what is in it, how each file is read, and what each tool does. How an application like this is
worked on — what is the machine's and what stays with a person — is
[Rule-Derived Test Design](https://github.com/reny-develop/rule-derived-test-design)'s to say,
and is not repeated here.

## Who reads what

The folder is made for somebody who builds the application in VS Code with
RulealizeStudio.AspNetCore, and who need not be an engineer. What they read is the application's
own pages, in a browser and in **Test cases**: every situation the rules allow shown as the pages
the application serves, with what a change did shown as the pages before it and after it; and each
specification, drawn as a diagram, sentences and a table. Where no
screen shows a rule set, the page is the one drawn from its rules, and each step is said in the
rules' own words beside it. They do not read any file here as text. The rule sets, the test designs,
the label documents, Rulealize, Ruledger, Razor and JSON are not words of theirs: what there is to
say to them about the application is said in the words of its pages and its specifications.

## What is in the folder

| | |
| --- | --- |
| `<name>.razor` | a screen: a page at the address its `@page` gives, showing the model of one rule set; there may be none |
| `<name>.razor.css` | how one screen looks, each element by a class of its own, with a `@media (max-width: …px)` block for each narrower view; Blazor keeps it to that screen; there may be none |
| `specification.json`, and any other `<name>.specification.json` | what the application should do, as UML state machines, each element bound to the rules that carry it out |
| `<rule set>.json` | the rules; there may be more than one |
| `<rule set>.test-design.json` | everything the rules allow, as `dotnet run -- derive` writes it down |
| `<rule set>.labels.<language>.json` | what the application says, in one language, where the rules refuse with a code |
| `<rule set>.state.json` | where the rule set's positions start, where its initial state is only the shape of a case |
| `appsettings.json` | where the positions are kept, and the address of a rule set no screen shows; neither is needed |
| `wwwroot/app.css` | how every page looks, linked from each where it is there |
| `views.json` | the widths the screens are designed at, a name and a width each, which the screen editor draws and writes the `@media` blocks for; where there is none, one view, PC at 1280 |
| the `.csproj` | the vocabularies the rule sets require, as packages |

There is no C# beyond `Program.cs`, which is the same in every application, and is not edited when
a screen or a rule set is added, and the parameters of a screen's address; the model each screen inherits is generated from its rule set when
the application is built. A rule set is any JSON file here whose `$schema` says it is one, and the
files beside it are named after it: `booking.json`, `booking.test-design.json`,
`booking.labels.en.json`. A rule set another holds through `uses` is a JSON file here too, found by
the `id` inside it, which is where `rulealize restore` writes it. A specification is any JSON file
here whose `$schema` says it is one; it is written before there are rules, and is named after none.
How many of each there are is not counted against the others: two screens may show one rule set, a
rule set may be shown by none, and one specification may ask for rules of two.

## The screens

A screen is a Razor component with an address, which inherits the model of the rule set it shows:

```razor
@page "/bookings/{Id}"
@inherits RuleScreen<BookingModel>

<p>@Model.State.Name</p>
<RuleButton Input="Model.ChooseSeat" Parameter="@("window")">Window</RuleButton>
<RuleButton Input="Model.Confirm" Actor="() => Model.Confirm.By">Confirm</RuleButton>
<RuleButton Command="Model.Back" Name="back">Undo</RuleButton>
<RuleForm Input="Model.SetName">
    <RuleField For="() => Model.SetName.To" />
    <button type="submit">Set</button>
    <ValidationMessage For="() => Model.SetName.To" />
</RuleForm>

@code {
    [Parameter]
    public string? Id { get; set; }
}
```

The position belongs to the address, not to whoever opens it: everybody at `/bookings/7` moves the
same booking, and every value of `{Id}` is a booking of its own, given to the screen as the
parameter of that name. A `RuleButton` applies an input, with
the value of its one settled parameter as `Parameter`, and is disabled while the rules do not allow
it; `Actor` gives the rules who is signed in as that parameter instead. A `RuleForm` applies an input
with what is typed into its `RuleField`s, each drawn as what the parameter's schema allows, and a
refusal is said beside the field it was about. Whether anything is shown is the rules' to decide —
`@if (Model.Confirm.IsOffered)`, `Model.IsTerminal` — and nothing on a screen decides anything else.
While the application is developed, `/signin` signs anybody in by the name they type.

**With no screen**, a rule set is a Web API, at `/api/<id>`, its `id` as the document says it, with one
position; while the application is developed, the page at `/`, where no screen is, is drawn from its
rules, to press the API with a browser, and its test design is walked on that page. Where it is a
resource, one position per booking, `appsettings.json` gives it an address of its own by its `id`, and
its page is the same address without `/api`:

```json
{ "Rulealize": { "Api": { "booking": "/api/bookings/{id}" } } }
```

Every screen's position is served over HTTP as well while the application is developed, at its
address under `/api`, and `/openapi/v1.json` describes all of them. Positions are kept in memory, or
in SQLite where `appsettings.json` has a connection string named `Positions`.

## The specification

A specification is a UML state machine: the states the application is in, the transitions
between them — each what a person does, and what it waits for — and notes on either, or on the
whole, for what is said about them that is not a move. Each of the three is an element, under an
id of its own, and holds what it says and the rules that carry it out, each by a name the rule set
gives it — never a place in the text:

```json
{
  "$schema": "rulealize-studio-aspnetcore/state-machine/v1",
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

A state may be `"final": true`, and a transition may have a `guard`, in words. What is said in an
element is the person's, in their words: whether there is one booking or one per address, who does
what. RulealizeStudio.AspNetCore's editor writes the file in one layout, whoever wrote it last;
nothing else binds it to the rules.

| a rule | its name |
| --- | --- |
| a state field and what its schema allows | `/state/schema/<field>` |
| an input | `/inputs/<input>` |
| a parameter of it, its guard, who may make it, its effects | `/inputs/<input>/params/<parameter>`, `/inputs/<input>/when`, `/inputs/<input>/actor`, `/inputs/<input>/effects` |
| a `validate` clause, by its code | `/inputs/<input>/validate/<code>` |
| a definition, a projection, the ending | `/definitions/<name>`, `/projections/<name>`, `/terminal` |

An element may be bound to nothing yet. Where the application has more than one rule set, a binding
names the one it is to, by its file without `.json`, before a `#`: `booking#/inputs/setParty`. A rule
set another holds through `uses` is not held to a specification; the rule set holding it is.

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

**The names a screen binds.** A screen binds the names its rule set gives, as the generated model
spells them — each word capitalised and anything between words dropped, so the input `setName` and
one called `set-name` are both bound as SetName:

```razor
@Model.State.Name                                  @* the state field name *@
<RuleField For="() => Model.SetName.To" />         @* setName's open parameter to *@
<RuleButton Input="Model.SetName">                 @* the input itself *@
@Model.Booking                                     @* the projection booking *@
<RuleButton Input="Model.SigsSign">                @* sign, of the rule set uses holds as sigs *@
```

A name a screen binds that the rules lack fails the build, and says which.

**What a refusal says.** A `validate` clause refuses with a code, and so does an open parameter's
`invalid`, for a value its schema does not admit. The sentence shown for either is in a label
document beside the rule set, keyed `/inputs/<input>/validate/<code>`; a code with no label is shown
as the code:

```json
{
  "$schema": "rulealize-studio-aspnetcore/labels/v1",
  "labels": { "/inputs/setParty/validate/party.unchanged": "The party is already that size." }
}
```

## The tools

Each command below is run in this folder, and is the program the RulealizeStudio.AspNetCore
extension runs to show the same thing: the application answers them itself, with its own
vocabularies, so there is no other tool to restore.

| command | what it does, and what it reports |
| --- | --- |
| `dotnet build` | builds the application, the model generated from each rule set; a name a screen binds that the rules lack is a build error |
| `dotnet run -- derive <rule set>.json` | walks the rules and writes the test design beside them, carrying the `edits` already in it; reports a choice it could not carry |
| `dotnet run -- replay <rule set>.test-design.json` | serves the application in memory, stands it in every state the test design names, presses the control for every move on the page that shows it, and tries every refused value; reports where the pages do not do what the design says, or do not say a refusal as its label document does |
| `dotnet run -- show <rule set>.test-design.json --out <file>` | writes every situation of the test design as the pages it pressed on |
| `dotnet run -- changes <rule set>.json --before <rule set as last committed> --design <rule set>.test-design.json` | says what the rules as they are now decide otherwise than the rules as last committed |
| `dotnet run -- agree` | compares every specification in the folder with every rule set in it; reports a binding to a rule the rules lack, a rule no element of any specification asks for, and an element a specification refers to and does not have |

They exit 0 when there is nothing to report, 1 when it could not be done, 2 when the command line
was not understood, and 3 when there is something to report.

Where a parameter is typed rather than picked from what the rules list — a name, say — the walk
follows only the values written in the design's `edits`, in the form Ruledger's
[an edit](https://github.com/reny-develop/Ruledger/blob/main/doc/test-design.md#an-edit) gives.
A value the rules refuse is written down as refused, with its code, and the choice is reported as
not carried.
