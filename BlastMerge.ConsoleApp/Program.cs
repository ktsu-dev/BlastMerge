// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Text;
using ktsu.BlastMerge.ConsoleApp.CLI;
using ktsu.BlastMerge.ConsoleApp.Services;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Services;
using Microsoft.Extensions.DependencyInjection;

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
			// Configure dependency injection
			ServiceCollection services = new();
			services.ConfigureBlastMergeServices();

			// Add console-specific services  
			services.AddSingleton<ConsoleApplicationService>();
			services.AddSingleton<IApplicationService>(provider => provider.GetRequiredService<ConsoleApplicationService>());
			services.AddSingleton<CommandLineHandler>();
			services.AddSingleton<FileComparisonDisplayService>();
			services.AddSingleton<ComparisonOperationsService>();
			services.AddSingleton<SyncOperationsService>();
			services.AddSingleton<InteractiveMergeService>();
			services.AddSingleton<AsyncApplicationService>();
			services.AddSingleton<FileDisplayService>();

			// Add menu handlers
			services.AddTransient<CompareFilesMenuHandler>();
			services.AddTransient<FindFilesMenuHandler>();
			services.AddTransient<HelpMenuHandler>();
			services.AddTransient<IterativeMergeMenuHandler>();
			services.AddTransient<SettingsMenuHandler>();
			services.AddTransient<BatchOperationsMenuHandler>();

			using ServiceProvider serviceProvider = services.BuildServiceProvider();

			// Get the command line handler from DI
			CommandLineHandler commandLineHandler = serviceProvider.GetRequiredService<CommandLineHandler>();

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
