// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Tool;

using Cli = ktsu.BlastMerge.Cli;

/// <summary>
/// Entry point for the <c>blastmerge</c> .NET tool.
/// </summary>
/// <remarks>
/// The tool ships the same application as <c>BlastMerge.Cli</c>; only the packaging
/// differs, so this head does nothing but hand the arguments to that entry point.
/// </remarks>
internal static class Program
{
	/// <summary>
	/// Runs the console application.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code - 0 for success, 1 for error.</returns>
	internal static int Main(string[] args) => Cli.Program.Main(args);
}
