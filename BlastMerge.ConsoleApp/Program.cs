// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Text;
using ktsu.BlastMerge.ConsoleApp.CLI;
using ktsu.BlastMerge.ConsoleApp.Services;

/// <summary>
/// Main entry point for the BlastMerge console application.
/// Handles user input and display only, delegating business logic to services.
/// </summary>
public static class Program
{
	/// <summary>
	/// Main entry point for the application.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code - 0 for success, 1 for error.</returns>
	public static int Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		Console.InputEncoding = Encoding.UTF8;

		try
		{
			// Create services with proper dependency injection pattern
			Core.Contracts.IApplicationService applicationService = new ConsoleApplicationService();
			CommandLineHandler commandLineHandler = new(applicationService);

			// Process command line arguments
			return commandLineHandler.ProcessCommandLineArguments(args);
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"Application error: {ex.Message}");
			return 1;
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine($"Invalid argument: {ex.Message}");
			return 1;
		}
	}
}
