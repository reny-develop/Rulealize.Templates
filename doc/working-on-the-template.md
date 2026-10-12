# Working on the template

For changing what `rulealize-plugin` writes, and [below](#rulealize-avalonia-app), what
`rulealize-avalonia-app` and `rulealize-aspnetcore-app` do. Nothing here is needed to use it — that is
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

## rulealize-avalonia-app

Tried the same way, from `./content/RulealizeAvaloniaApp`. The generated folder restores
RulealizeStudio's libraries, which are not on nuget.org yet, so it is generated with
`--feed` pointing at a folder holding them — `feed/` in a RulealizeStudio working copy, after
packing its `Binding`, `Hosting` and `Generator`:

```sh
dotnet new rulealize-avalonia-app -n Trial --feed <RulealizeStudio>/feed
cd Trial
dotnet build                                 # silent, and the window opens empty
dotnet tool restore                          # rulealize-studio from the same folder, ruledger from nuget.org
```

`sourceName` is `Rulealize.Avalonia.Example`, which is the project, the namespace and the window's
title. `--feed` replaces `FEED_FOLDER` in `nuget.config` and is
the only thing that writes it. The versions of RulealizeStudio's packages in the csproj, and of
`rulealize-studio` in `.config/dotnet-tools.json`, are the ones RulealizeStudio packs, and `--feed`
needs its server packed beside the libraries for `dotnet tool restore` to find it. `AGENTS.md`
names the version of nothing; what it says each check is held to RulealizeStudio's commands.

## rulealize-aspnetcore-app

Tried the same way, from `./content/RulealizeAspNetCoreApp`, with `--feed` pointing at
`feed/` in a RulealizeStudio.AspNetCore working copy, after packing its `Binding`, `Components`,
`Hosting` and `Generator`:

```sh
dotnet new rulealize-aspnetcore-app -n Trial --feed <RulealizeStudio.AspNetCore>/feed
cd Trial
dotnet build                                 # silent
dotnet run                                   # serves nothing yet: / is 404
```

Then a rule set and its vocabularies — a sample's from RulealizeStudio.AspNetCore — so that
`dotnet run -- derive <rule set>.json` and `dotnet run -- replay <rule set>.test-design.json` walk
it on the page drawn from its rules, and once more with the sample's screen beside it.

`sourceName` is `Rulealize.AspNetCore.Example`, which is the project, the namespace and the launch
profile. `--feed` replaces `FEED_FOLDER` in `nuget.config`, and the port in `launchSettings.json`
is generated. The versions of RulealizeStudio.AspNetCore's packages in the csproj are the ones it
packs. There is no tool manifest: the application answers what the editor asks on its own command line.

## Releasing

`Version` in `Rulealize.Templates.csproj`, then `dotnet pack -c Release` and push. The
repository url and commit go into the nuspec from the build, so **pack after committing** or
the package points at a commit that does not contain it.

The readme is packed as the nuget.org landing page, which means a correction to it only
reaches that page in a new version.
