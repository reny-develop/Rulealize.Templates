# Writing the rules of this application

This folder is an Avalonia application made of a rule set and XAML. Somebody has written down
what it should look like and what it should do; your part is the rules that make it do that.
What you write is read by them as the test design, on their own screen, and they say whether it
is what they meant. Nothing you say about your own work stands in for that.

## What is in the folder

| | |
| --- | --- |
| `MainWindow.axaml` | the design: the window, bound by name to what the rules give |
| `specification.md`, or any other specification | what the application should do, a part at a time |
| `<rule set>.json` | the rules, which you write |
| `<rule set>.blueprint.json` | each part of each specification, bound to the rules that carry it out |
| `<rule set>.test-design.json` | everything the rules allow, as `ruledger derive` writes it down |
| the `.csproj` | the vocabularies the rule set requires, as packages |

The design and the specifications together are the blueprint. Nothing else is added to the folder.
There is no C# to write: `Program.cs` is the same in every application and is not edited, and the
model the window binds to is generated from the rule set when the application is built. The rule
set is any JSON file here whose `$schema` says it is one, and the blueprint and the test design
are named after it: `booking.json`, `booking.blueprint.json`, `booking.test-design.json`.

## The blueprint comes first

Every rule is there because a part of the blueprint asks for it, and is bound to that part. A
rule nothing asks for is not written. Where you find the rules need something the blueprint does
not say — a bound it left out, a case it did not think of — write it into the specification first,
as a part of its own, tell the person you did, bind it, and only then write the rule. Then
somebody who reads the blueprint has read everything the application does.

In Markdown a part is a paragraph, list item, heading or table row that ends with a comment
naming it:

```markdown
- A party is one to six people. <!-- part: party-size -->
```

The blueprint binds each part, by its id, to the rules that carry it out, each by a name the rule
set gives it — never a place in the text:

```jsonc
{
  "$schema": "rulealize-studio/blueprint/v1",
  "specifications": {
    "specification.md": {
      "party-size": ["/state/schema/party", "/inputs/setParty", "/inputs/setParty/params/size"],
      "party-unchanged": ["/inputs/setParty/validate/party.unchanged"]
    }
  }
}
```

| a rule | its name |
| --- | --- |
| a state field and what its schema allows | `/state/schema/<field>` |
| an input | `/inputs/<input>` |
| a parameter of it, its guard, who may make it, its effects | `/inputs/<input>/params/<parameter>`, `/inputs/<input>/when`, `/inputs/<input>/actor`, `/inputs/<input>/effects` |
| a `validate` clause, by its code | `/inputs/<input>/validate/<code>` |
| a definition, a projection, the ending | `/definitions/<name>`, `/projections/<name>`, `/terminal` |

A part bound to nothing yet is a requirement nothing carries out, and is what you are here to
change.

## Writing the rule set

How a rule set is written is Rulealize's to say, and is not repeated here:
[a rule set in five minutes](https://github.com/reny-develop/Rulealize/blob/main/doc/ruleset-quickguide.md),
then [writing a rule set](https://github.com/reny-develop/Rulealize/blob/main/doc/ruleset-guide.md)
for the whole language, and [the runtime's semantics](https://github.com/reny-develop/Rulealize/blob/main/doc/runtime.md)
for what it does with one.

**The vocabularies it loads.** The operations a rule set uses come from vocabularies, which its
`requires` names, with the versions that will do;
[vocabulary](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md) says where the
published ones are listed, and what each one provides is its own specification. The application
loads each one as a package: for every vocabulary `requires` names, a `PackageReference` in the
`.csproj`'s last `ItemGroup`, at a version `requires` allows. That `ItemGroup` is the one place in
the folder you write to rather than add to.

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

Give exactly the names the design binds; a name it binds that the rules lack fails the build and
says which.

## How you know it is finished

Each of these is one command, run in this folder, and exits 0 when it has nothing to report. They
are what the editor shows the person — the same programs, in the Problems panel, in the rules'
own editor and as this folder's tasks — so a check that passes here passes there. Once, first:

```sh
dotnet tool restore
```

Then, after the last change, in this order:

1. **The design builds against the rules.**
   `dotnet build`
2. **The rule set compiles,** against the vocabularies the build put beside the application.
   `dotnet rulealize-studio check`
3. **The blueprint and the rules agree:** no binding to a rule the rules lack, and no rule that
   no part asks for.
   `dotnet rulealize-studio agree`
4. **The test design is the rules' as they are now.** `ruledger derive` writes it, carrying the
   values somebody chose to try; `ruledger diff` says whether it is still what the rules do.
   `dotnet ruledger derive <rule set>.json --plugins bin/Debug/net10.0`
   `dotnet ruledger diff <rule set>.test-design.json <rule set>.json --plugins bin/Debug/net10.0`

   Where a value is typed rather than picked from what the rules list — a name, say — the walk
   follows only the values somebody chose to try, and until one is chosen it stops there: a
   design of one state passes every check here and shows the person nothing. So choose them,
   values the rules take: each is an entry in the design's `edits`, written the way
   [an edit](https://github.com/reny-develop/Ruledger/blob/main/doc/test-design.md#an-edit)
   says, and deriving again carries it. Tell the person which you chose; they are theirs to
   change.
5. **The screen does what the test design says,** on the application as step 1 built it: every
   state it names, every move pressed on the control that stands for it.
   `dotnet rulealize-studio replay`

Each says what it found the way a compiler does — the file, the place, what is wrong there. Put
right what it says where it says it, and start again from the first.

Then stop, and do not commit. The person reads the test design, and committing it is their yes.
