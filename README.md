# Rulealize.Templates

`dotnet new` templates for [Rulealize](https://github.com/reny-develop/Rulealize).

```sh
dotnet new install Rulealize.Templates
dotnet new rulealize-plugin -n Rulealize.Plugin.Text
```

| Template | |
| --- | --- |
| `rulealize-plugin` | a vocabulary: one plugin class, one worked operation, and a rule set that calls it |

## What comes out

```
Rulealize.Plugin.Text/
├─ .config/dotnet-tools.json          the rulealize command, pinned
├─ README.md  LICENSE  .editorconfig  .gitattributes  .gitignore
├─ Rulealize.Plugin.Text.slnx
├─ doc/specification.md               a skeleton, with the Decided section it needs
├─ ruleset/probe.json                 the smallest rule set that calls this vocabulary
└─ src/Rulealize.Plugin.Text/
   ├─ Rulealize.Plugin.Text.csproj
   ├─ TextPlugin.cs                   named, namespaced, and registering the example
   └─ ExampleNode.cs                  the operation to copy, and then delete
```

And it runs, before anything is written:

```sh
cd Rulealize.Plugin.Text
dotnet tool restore
dotnet build                                 # drops the assembly into plugin/
dotnet rulealize plugins                     # Rulealize.Plugin.Text 1.0.0 (text) -- text.example
dotnet rulealize restore ruleset/probe.json  # fetches the standard vocabularies it also names
dotnet rulealize play ruleset/probe.json     # walks it
```

Building copies the assembly into `plugin/`, which is the folder `rulealize` reads and the
arrangement a deployed application has. **A vocabulary under development and one fetched
from nuget.org sit in it side by side**, so `restore` credits what is already there and
fetches only the rest.

## Parameters

| | |
| --- | --- |
| `-n <name>` | the plugin identifier, the project name and the package id. `Rulealize.Plugin.Text` |
| `--namespace <ns>` | the operation namespace, which every operation is prefixed with. Defaults to the last part of the name, lowercased — `Rulealize.Plugin.Text` gives `text` |

The default suits `Rulealize.Plugin.*`. A vendor-qualified identifier wants it given:
`-n Acme.Deploy.Rules --namespace acme`, because the last part of that name is `rules` and
[the conventions](https://github.com/reny-develop/Rulealize/blob/main/doc/plugin.md#the-conventions)
ask for the vendor.

## The example is meant to be deleted

`ExampleNode.cs` measures the length of a text, which has nothing to do with whatever you
are building. Nothing in `-n` or `--namespace` could say what your first operation should
do, so what is generated is a worked example rather than a guess: the shortest operation in
which all four steps of an `Evaluate` are visible at once. Copy its shape, write yours,
delete it.

The same goes for `ruleset/probe.json`, which calls it.

## Working on these templates

```sh
dotnet new install C:\Repos\Rulealize.Templates\content\RulealizePlugin
dotnet new rulealize-plugin -n Rulealize.Plugin.Trial
dotnet new uninstall C:\Repos\Rulealize.Templates\content\RulealizePlugin
```

A template installs from a folder, so nothing has to be packed or published to try a change.

## License

Apache-2.0.
