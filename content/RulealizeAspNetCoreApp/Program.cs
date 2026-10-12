using RulealizeStudio.AspNetCore.Hosting;

namespace Rulealize.AspNetCore.Example;

/// <summary>The entry point, and the only C# this application has.</summary>
public static class Program
{
    /// <summary>Starts the application, or answers its command line: derive, show, replay, changes.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>The exit code.</returns>
    public static int Main(string[] args) => RuleApp.Run(typeof(Program).Assembly, args);
}
