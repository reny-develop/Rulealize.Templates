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

The generated project is the thing to check, not the template source. Two restores come
before anything of it runs — the pinned tool, and the four vocabularies `probe.json` names
besides its own:

```sh
cd Rulealize.Plugin.Trial
dotnet tool restore
dotnet build                                 # silent, or the change is not done
dotnet rulealize plugins                     # what registered, and under which namespace
dotnet rulealize restore ruleset/probe.json  # check and play need this; plugins does not
dotnet rulealize check ruleset/probe.json
dotnet rulealize play ruleset/probe.json
```

Generate under a second name as well — `-n Acme.Deploy.Rules --namespace acme` — because
that is the path where every token substitution actually moves.

**What a folder install cannot tell you is whether the package is intact.** It reads the
folder directly, so it never runs the csproj's `Content` globs or `NoDefaultExcludes`, and
what those exist for is the dot files, `.config/dotnet-tools.json` and `.template.config`
itself — each one something the generated project is broken without, and none of them
visible as missing from this side. The folder route succeeds either way. When one of them
has been touched, check a packed package rather than the folder.

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
