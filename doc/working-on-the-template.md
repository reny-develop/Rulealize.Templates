# Working on the template

For changing what `rulealize-plugin` writes. Nothing here is needed to use it — that is
`dotnet new install Rulealize.Templates`, and the [readme](../README.md) covers it.

## Trying a change

A template installs from a folder, so a change can be tried without packing or publishing
anything. From the root of a working copy:

```sh
dotnet new install ./content/RulealizePlugin
dotnet new rulealize-plugin -n Rulealize.Plugin.Trial   # somewhere outside this repository
dotnet new uninstall ./content/RulealizePlugin
```

Installing again over the same folder wants `--force`, since the version has not moved.

The generated project is the thing to check, not the template source: `dotnet build` there
should be silent, and `rulealize plugins`, `check` and `play` should all run against
`ruleset/probe.json` before a change is called done. Generate under a second name as well —
`-n Acme.Deploy.Rules --namespace acme` — because that is the path where every token
substitution actually moves.

## Trying the package

The folder route skips packing, so it cannot catch a file the pack leaves out. When the
`.template.config`, the dot files or the `Content` globs in the csproj have been touched,
go the whole way:

```sh
dotnet pack -c Release -o ./feed
dotnet new install Rulealize.Templates --add-source ./feed
```

`dotnet new install <id>` resolves the id against the configured NuGet sources, so a folder
named with `--add-source` behaves exactly as nuget.org will.

## What the substitutions are

`.template.config/template.json` drives three of them, and all three are string replacements
over the content:

| | |
| --- | --- |
| `Rulealize.Plugin.Example` | `sourceName`. The project name, the package id, the folder and the plugin identifier |
| `yourns` | the operation namespace, from `--namespace` or the last part of the name, lowercased |
| `Vocab` | the plugin class, so `VocabPlugin.cs` becomes `<LastPart>Plugin.cs` |

The third is the one to keep in mind while writing prose for the template: **`Vocab` is
replaced wherever it appears**, so a sentence opening with "Vocabulary" would be mangled and
a lowercase "vocabulary" is safe. Both the generated readme and the specification say
"vocabulary" many times, deliberately, and never at the start of a sentence.

## Releasing

`Version` in `Rulealize.Templates.csproj`, then `dotnet pack -c Release` and push. The
repository url and commit go into the nuspec from the build, so **pack after committing** or
the package points at a commit that does not contain it.

The readme is packed as the nuget.org landing page, which means a correction to it only
reaches that page in a new version.
