# Rulealize.Templates

`dotnet new` templates for [Rulealize](https://github.com/reny-develop/Rulealize).

```sh
dotnet new install Rulealize.Templates
dotnet new rulealize-plugin -n Rulealize.Plugin.Example
```

| Template | |
| --- | --- |
| `rulealize-plugin` | a vocabulary: one plugin class, one worked example of each kind of node, and a rule set that calls them |
| `rulealize-avalonia-app` | an Avalonia application made of a rule set and XAML, before either is written — [below](#an-avalonia-application) |
| `rulealize-aspnetcore-app` | an ASP.NET Core application made of rule sets and Razor screens, before either is written — [below](#an-aspnet-core-application) |

The scaffolding is what a template can write. The part it cannot is
[**writing a vocabulary**](doc/writing-a-vocabulary.md) — what `Build` and `Evaluate` are
for, how to refuse bad input, what a value is, what the fourth kind of operation is, and
[what publishing changes](doc/writing-a-vocabulary.md#when-you-publish-it): the namespace
stops being yours alone to decide, and is claimed in an index rather than in your assembly.

`Rulealize.Plugin.Example` above is a name chosen to be thrown away; give yours the name you
mean to keep.

## What comes out

```
Rulealize.Plugin.Example/
├─ .config/dotnet-tools.json          the rulealize command, pinned
├─ README.md  .editorconfig  .gitattributes  .gitignore
├─ Rulealize.Plugin.Example.slnx
├─ doc/specification.md               a skeleton, with the Decided section it needs
├─ ruleset/probe.json                 the smallest rule set that calls this vocabulary and
│                                     still takes three moves to finish, annotated slot by
│                                     slot with the kind of node each one takes
└─ src/Rulealize.Plugin.Example/
   ├─ Rulealize.Plugin.Example.csproj
   ├─ ExamplePlugin.cs                named, namespaced, and registering the three below
   ├─ TopNode.cs                      an expression, to copy and then delete
   ├─ PushNode.cs                     an effect, likewise
   └─ PileSchemaNode.cs               a schema, likewise
```

No `LICENSE`, and no licence declared in the csproj. Which terms a vocabulary ships under is
its author's to decide, and a template that guessed would be putting somebody else's name in
your repository.

And it runs, before anything is written:

```sh
cd Rulealize.Plugin.Example
dotnet tool restore
dotnet build                                 # drops the assembly into plugin/
dotnet rulealize plugins                     # Rulealize.Plugin.Example 1.0.0 (example) -- 3 operations
dotnet rulealize restore ruleset/probe.json  # fetches the vocabularies it also names
dotnet rulealize play ruleset/probe.json     # walks it
```

```
    1. push(token: red)
    2. push(token: green)
    3. push(token: blue)
> 1

    1. push(token: green)
    2. push(token: blue)
>
```

Three moves, a state that grows with each one, and a guard of yours removing the option that
has just been taken. `rulealize moves` and `rulealize apply` do the same one step at a time,
against a state document you can keep.

Building copies the assembly into `plugin/`, which is the folder `rulealize` reads and the
arrangement a deployed application has. **A vocabulary under development and one fetched
from nuget.org sit in it side by side**, so `restore` credits what is already there and
fetches only the rest.

## Parameters

| | |
| --- | --- |
| `-n <name>` | the plugin identifier, the project name and the package id. `Rulealize.Plugin.Example` |
| `--namespace <ns>` | the operation namespace, which every operation is prefixed with. Defaults to the last part of the name, lowercased — `Rulealize.Plugin.Example` gives `example` |

The default suits `Rulealize.Plugin.*`. A vendor-qualified identifier wants it given:
`-n Acme.Deploy.Rules --namespace acme`, because the last part of that name is `rules` and
[the conventions](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md#the-identifier-and-the-namespace-are-still-claimed)
ask for the vendor.

## The examples are meant to be deleted

Three files, one per kind of node, and between them a pile of tokens: `example.pile` says what
a state field holds, `example.push` writes a token to it, `example.top` reads the last one
back. That has nothing to do with whatever you are building, and it is not meant to. Nothing in
`-n` or `--namespace` could say what your first operation should do, so what is generated is
a worked example rather than a guess — the shortest three in which an `Evaluate`, an `Apply`
and five of a schema's members are each visible whole.

Copy the shape you need, write yours, delete all three. The same goes for
`ruleset/probe.json`, which calls them.

## An Avalonia application

```sh
dotnet new rulealize-avalonia-app -n Booking --feed <folder>
```

```
Booking/
├─ Booking.csproj      the vocabularies the rule set requires, as packages; none yet
├─ Program.cs          one call, naming neither the rule set nor its model
├─ MainWindow.axaml    a window with nothing on it
├─ specification.json  what the application should do, as a UML state machine; nothing in it yet
├─ AGENTS.md           what is in the folder and what each tool does; CLAUDE.md only reads it
├─ .config/dotnet-tools.json   rulealize-studio, pinned
├─ .vscode/tasks.json  the build and the replay, as tasks
└─ .gitignore
```

It builds as it is and opens an empty window. What is added to it afterwards is rule sets — any
JSON file in the folder that says it is one — windows, `MainWindow.axaml` and any other `.axaml`,
each bound to the model generated from one of them and shown while the rules say, specifications
whose states, transitions and notes are each bound to the rules that carry them out, and the test
design beside each rule set, with a label document per language for what its refusals say; none
counted against the others, and `Program.cs` not edited for any of them.
`AGENTS.md` says who reads what — the person the folder is made for reads its windows, its
specifications and **Test cases** in RulealizeStudio.Avalonia, and none of its files as text —
what is in the folder, how each file is read, and what each tool does and reports
— `dotnet build`, and `rulealize-studio check`, `agree`, `derive`, `replay`, `show` and `changes`
— each the program the editor runs to show the person the same thing. It says nothing about
what an agent should do: how an application like this is worked on is
[Rule-Derived Test Design](https://github.com/reny-develop/rule-derived-test-design)'s to say, and
it links there rather than restating it, as it links Rulealize's guides. That is
[RulealizeStudio](https://github.com/reny-develop/RulealizeStudio.Avalonia)'s arrangement, and
the libraries it references are RulealizeStudio's.

**`--feed` is there while those libraries are not on nuget.org.** Given a folder of packages, the
template writes a `nuget.config` that restores from it as well; without it, no `nuget.config` is
written. The extension that makes an application folder passes its own. Once the libraries are
published, the parameter and the file go.

## An ASP.NET Core application

```sh
dotnet new rulealize-aspnetcore-app -n Booking --feed <folder>
```

```
Booking/
├─ Booking.csproj      the vocabularies the rule sets require, as packages; none yet
├─ Program.cs          one call, naming neither a rule set nor its model
├─ _Imports.razor      what a screen names without saying where it is from
├─ Properties/launchSettings.json   Development, on a port of its own
├─ specification.json  what the application should do, as a UML state machine; nothing in it yet
├─ AGENTS.md           what is in the folder and what each tool does; CLAUDE.md only reads it
└─ .gitignore
```

It builds as it is and serves nothing: there is no screen and no rule set. What is added to it
afterwards is rule sets — any JSON file in the folder that says it is one — screens, Razor
components with an address, each showing the model generated from one of them, and specifications
whose states, transitions and notes are each bound to the rules that carry them out; a rule set no
screen shows is a Web API, and while the application is developed it is pressed on a page drawn
from its rules. With them come the test design beside each rule set, a label document per language
for what its refusals say, and an `appsettings.json` where one is wanted; none counted against the
others, and `Program.cs` not edited for any of them. `AGENTS.md` says who reads what — the person the
folder is made for reads its pages, its specifications and **Test cases** in RulealizeStudio.AspNetCore,
and none of its files as text — what is in the folder, how each file is read, and what each tool does
and reports: `dotnet build`, and `derive`, `replay`, `show`, `changes` and `agree`, which the application answers on its own
command line in its Debug configuration, from a package it references there only so that none of it
is published, each the program the editor runs to show the person the same thing. So there are no
tools to restore. Like the Avalonia application's, it says nothing about what an agent should do.

`--feed` is there for the same reason, for RulealizeStudio.AspNetCore's libraries.

## License

Apache-2.0. That covers this repository; what the template writes into yours is yours, and
carries no licence of ours.

Changing what the template writes is
[working on the template](doc/working-on-the-template.md).
