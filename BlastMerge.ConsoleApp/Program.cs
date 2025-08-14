// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Text;
using ktsu.BlastMerge;
using ktsu.BlastMerge.ConsoleApp.CLI;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Services;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;
using ktsu.BlastMerge.Contracts;
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

		// Configure dependency injection
		ServiceCollection services = new();
		services.ConfigureBlastMergeServices();

		// Add console-specific services
		services.AddTransient<IApplicationService, ConsoleApplicationService>();
		services.AddTransient<IInputHistoryService, InputHistoryService>();
		services.AddTransient<CommandLineHandler>();
		services.AddTransient<FileComparisonDisplayService>();
		services.AddTransient<ComparisonOperationsService>();
		services.AddTransient<SyncOperationsService>();
		services.AddTransient<InteractiveMergeService>();
		services.AddTransient<AsyncApplicationService>();
		services.AddTransient<FileDisplayService>();

		// Add menu handlers
		services.AddTransient<CompareFilesMenuHandler>();
		services.AddTransient<FindFilesMenuHandler>();
		services.AddTransient<HelpMenuHandler>();
		services.AddTransient<IterativeMergeMenuHandler>();
		services.AddTransient<SettingsMenuHandler>();
		services.AddTransient<BatchOperationsMenuHandler>();

		using ServiceProvider serviceProvider = services.BuildServiceProvider();

		CommandLineHandler commandLineHandler = serviceProvider.GetRequiredService<CommandLineHandler>();

		return commandLineHandler.ProcessCommandLineArguments(args);
	}
}
