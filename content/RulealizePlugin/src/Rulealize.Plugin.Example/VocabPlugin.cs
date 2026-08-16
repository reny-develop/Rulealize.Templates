// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;

namespace Rulealize.Plugin.Example
{
    /// <summary>
    /// This vocabulary: its name, and the list of what it provides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runtime finds this class by sweeping a folder of assemblies for public types with
    /// a parameterless constructor that implement <see cref="IRulealizePlugin"/>. It fails
    /// all four conditions quietly, so if a build lands here and nothing loads, ask
    /// <c>rulealize plugins</c> which one.
    /// </para>
    /// </remarks>
    public sealed class VocabPlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        /// <remarks>
        /// Three things. The identifier is what a rule set's <c>requires</c> names. The
        /// version is what that entry's <c>^1.0</c> is checked against, and is not the
        /// package version next door in the csproj — raise both. The namespace is what every
        /// operation below is prefixed with, and this vocabulary cannot register into
        /// anybody else's.
        /// </remarks>
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.Example", new Version(1, 0, 0), "yourns");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            // On the left the name as JSON writes it, with the namespace added for you. On
            // the right the class that builds it. Adding an operation means adding a line
            // here, and this is the line that gets forgotten -- `rulealize plugins` lists
            // what really registered, so a name missing from it is missing from here.
            registry.AddExpression("example", ExampleNode.Build);
        }
    }
}
