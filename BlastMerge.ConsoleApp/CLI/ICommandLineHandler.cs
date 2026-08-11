// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.ConsoleApp.CLI;

/// <summary>
/// Interface for handling command line argument processing.
/// </summary>
public interface ICommandLineHandler
{
	/// <summary>
	/// Processes command line arguments and executes the appropriate action.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code - 0 for success, 1 for error.</returns>
	public int ProcessCommandLineArguments(string[] args);
}
