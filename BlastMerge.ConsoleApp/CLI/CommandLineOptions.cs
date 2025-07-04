// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.CLI;

using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

/// <summary>
/// Command line options for BlastMerge console application
/// </summary>
[Verb("blastmerge", HelpText = "Cross-Repository File Synchronization Tool")]
public class CommandLineOptions
{
	/// <summary>
	/// Show version information
	/// </summary>
	[Option('v', "version", HelpText = "Display version information and exit")]
	public bool ShowVersion { get; set; }

	/// <summary>
	/// Show help information
	/// </summary>
	[Option('h', "help", HelpText = "Display this help screen and exit")]
	public bool ShowHelp { get; set; }

	/// <summary>
	/// Directory path to search for files
	/// </summary>
	[Value(0, MetaName = "directory",
		HelpText = "Directory path to search for files. If provided with filename, runs in batch mode",
		Required = false)]
	public string? Directory { get; set; }

	/// <summary>
	/// Filename to search for in the directory
	/// </summary>
	[Value(1, MetaName = "filename",
		HelpText = "Filename to search for in the directory. Used with directory for batch mode",
		Required = false)]
	public string? FileName { get; set; }

	/// <summary>
	/// Batch configuration name to run
	/// </summary>
	[Option('b', "batch", HelpText = "Run a saved batch configuration")]
	public string? BatchName { get; set; }

	/// <summary>
	/// List all available batch configurations
	/// </summary>
	[Option('l', "list-batches", HelpText = "List all saved batch configurations")]
	public bool ListBatches { get; set; }

	/// <summary>
	/// Examples usage text
	/// </summary>
	[Usage(ApplicationAlias = "BlastMerge.exe")]
	public static IEnumerable<Example> Examples
	{
		get
		{
			return
			[
				new("Start interactive mode", new CommandLineOptions { }),
				new("Process files directly", new CommandLineOptions { Directory = "C:\\Projects", FileName = "README.md" }),
				new("Run a batch configuration", new CommandLineOptions { Directory = "C:\\Projects", BatchName = "Common Repository Files" }),
				new("List batch configurations", new CommandLineOptions { ListBatches = true }),
				new("Show version", new CommandLineOptions { ShowVersion = true }),
				new("Show help", new CommandLineOptions { ShowHelp = true })
			];
		}
	}
}
