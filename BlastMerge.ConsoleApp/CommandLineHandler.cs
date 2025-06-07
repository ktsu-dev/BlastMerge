// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine;
using ktsu.BlastMerge.ConsoleApp.Contracts;

/// <summary>
/// Handles command line argument processing and delegates to appropriate operations.
/// </summary>
/// <param name="applicationService">The application service for processing operations.</param>
public class CommandLineHandler(Core.Contracts.IApplicationService applicationService) : ICommandLineHandler
{
	/// <summary>
	/// Processes command line arguments and executes the appropriate action.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code - 0 for success, 1 for error.</returns>
	public int ProcessCommandLineArguments(string[] args)
	{
		ArgumentNullException.ThrowIfNull(args);
		ArgumentNullException.ThrowIfNull(applicationService);

		try
		{
			using Parser parser = new(with => with.HelpWriter = Console.Error);
			return parser
				.ParseArguments<CommandLineOptions>(args)
				.MapResult(
					ExecuteCommand,
					HandleParsingErrors
				);
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"Command parsing error: {ex.Message}");
			return 1;
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine($"Invalid command argument: {ex.Message}");
			return 1;
		}
	}

	/// <summary>
	/// Executes the appropriate command based on the parsed options.
	/// </summary>
	/// <param name="options">The parsed command line options.</param>
	/// <returns>Exit code - 0 for success, 1 for error.</returns>
	private int ExecuteCommand(CommandLineOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		try
		{
			// Handle version request
			if (options.ShowVersion)
			{
				ShowVersion();
				return 0;
			}

			// Handle help request
			if (options.ShowHelp)
			{
				ShowHelp();
				return 0;
			}

			// Handle list batches request
			if (options.ListBatches)
			{
				applicationService.ListBatches();
				return 0;
			}

			// Handle batch processing
			if (!string.IsNullOrEmpty(options.BatchName) && !string.IsNullOrEmpty(options.Directory))
			{
				applicationService.ProcessBatch(options.Directory, options.BatchName);
				return 0;
			}

			// Handle direct file processing
			if (!string.IsNullOrEmpty(options.Directory) && !string.IsNullOrEmpty(options.FileName))
			{
				applicationService.ProcessFiles(options.Directory, options.FileName);
				return 0;
			}

			// No specific command provided - start interactive mode
			applicationService.StartInteractiveMode();
			return 0;
		}
		catch (DirectoryNotFoundException ex)
		{
			Console.WriteLine($"Directory not found: {ex.Message}");
			return 1;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine($"Access denied: {ex.Message}");
			return 1;
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine($"Invalid parameter: {ex.Message}");
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"Command execution error: {ex.Message}");
			return 1;
		}
	}

	/// <summary>
	/// Handles command line parsing errors.
	/// </summary>
	/// <param name="errors">The parsing errors.</param>
	/// <returns>Exit code indicating error.</returns>
	private static int HandleParsingErrors(IEnumerable<Error> errors)
	{
		ArgumentNullException.ThrowIfNull(errors);
		// Help and version errors are handled by CommandLineParser automatically
		return 1;
	}

	/// <summary>
	/// Shows version information.
	/// </summary>
	private static void ShowVersion()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "Unknown";
		Console.WriteLine($"BlastMerge v{version}");
	}

	/// <summary>
	/// Shows help information.
	/// </summary>
	private static void ShowHelp()
	{
		Console.WriteLine("BlastMerge - Cross-Repository File Synchronization Tool");
		Console.WriteLine();
		Console.WriteLine("Intelligent iterative merging for multiple file versions across repositories.");
		Console.WriteLine("Automatically discovers, groups, and merges files using similarity-based progression.");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  BlastMerge.exe                              Start interactive mode (recommended)");
		Console.WriteLine("  BlastMerge.exe <directory> <filename>       Process files directly");
		Console.WriteLine("  BlastMerge.exe <directory> -b <batch>       Run batch configuration");
		Console.WriteLine("  BlastMerge.exe -l                           List batch configurations");
		Console.WriteLine("  BlastMerge.exe -v                           Show version");
		Console.WriteLine("  BlastMerge.exe -h                           Show this help");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  BlastMerge.exe                              Launch interactive menu");
		Console.WriteLine("  BlastMerge.exe C:\\Projects README.md        Find and merge all README.md files");
		Console.WriteLine("  BlastMerge.exe C:\\Repos -b \"Config Files\"   Run saved batch configuration");
	}
}
