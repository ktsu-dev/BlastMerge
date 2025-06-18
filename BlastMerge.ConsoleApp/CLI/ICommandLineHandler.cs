// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
