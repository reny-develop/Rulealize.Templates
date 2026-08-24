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
        /// <para>
        /// Three things. The identifier is what a rule set's <c>requires</c> names. The
        /// version is what that entry's <c>^1.0</c> is checked against, and is not the
        /// package version next door in the csproj — raise both. The namespace is what every
        /// operation below is prefixed with, and this vocabulary cannot register into
        /// anybody else's.
        /// </para>
        /// <para>
        /// A fourth is optional and left out here: the one character this vocabulary claims
        /// for shorthand, the way <c>state</c> claims the <c>$</c> in <c>"$pile"</c>.
        /// Claiming one is that argument plus an <see cref="ISugarExpander"/> handed to
        /// <c>registry.AddSugar</c> below, and until then the <c>Reserved prefix</c> row in
        /// the readme and the specification reads <c>none</c>. A character is not owned, so
        /// one another vocabulary already reserves is allowed.
        /// </para>
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
            //
            // Three calls for the three kinds of node, and the call is what decides the kind:
            // where an operation may appear is settled by which of these registered it, not
            // by anything the class says about itself.
            registry.AddSchema("pile", PileSchemaNode.Build);
            registry.AddEffect("push", PushNode.Build);
            registry.AddExpression("top", TopNode.Build);
        }
    }
}
