// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Text;
using System.Threading.Tasks;
using ktsu.BlastMerge.ConsoleApp.CLI;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Services;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
	public static async Task<int> Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		Console.InputEncoding = Encoding.UTF8;

		try
		{
			// Set up dependency injection
			IHost host = CreateHostBuilder(args).Build();

			// Get the command line handler from DI
			CommandLineHandler commandLineHandler = host.Services.GetRequiredService<CommandLineHandler>();

			// Process command line arguments
			return await commandLineHandler.ProcessCommandLineArgumentsAsync(args).ConfigureAwait(false);
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

	/// <summary>
	/// Creates the host builder for dependency injection.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>The host builder.</returns>
	private static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureServices((context, services) =>
			{
				// Register BlastMerge services
				services.AddBlastMergeServices();

				// Register console app services with interfaces
				services.AddSingleton<IAppDataHistoryInput, AppDataHistoryInput>();
				services.AddSingleton<IComparisonOperationsService, ComparisonOperationsService>();
				services.AddSingleton<ISyncOperationsService, SyncOperationsService>();
				services.AddSingleton<IFileComparisonDisplayService, FileComparisonDisplayService>();
				services.AddSingleton<IInteractiveMergeService, InteractiveMergeService>();
				services.AddSingleton<IUserInterfaceService, UserInterfaceService>();
				services.AddSingleton<IApplicationService, ConsoleApplicationService>();
				services.AddSingleton<CommandLineHandler>();

				// Register menu handlers
				services.AddSingleton<FindFilesMenuHandler>();
				services.AddSingleton<BatchOperationsMenuHandler>();
				services.AddSingleton<CompareFilesMenuHandler>();
				services.AddSingleton<IterativeMergeMenuHandler>();
				services.AddSingleton<SettingsMenuHandler>();
				services.AddSingleton<HelpMenuHandler>();
			});
}
