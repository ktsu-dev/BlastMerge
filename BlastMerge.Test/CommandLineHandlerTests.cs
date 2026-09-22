// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.CLI;
using ktsu.BlastMerge.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for <see cref="CommandLineHandler"/>, covering how each set of arguments is
/// dispatched and what the handler writes to the console.
/// </summary>
/// <remarks>
/// Console output is redirected per test, so these cannot share a process with tests that read
/// or write the console concurrently.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class CommandLineHandlerTests
{
	/// <summary>
	/// Records which application service operation the handler dispatched to.
	/// </summary>
	private sealed class RecordingApplicationService : IApplicationService
	{
		public List<string> Calls { get; } = [];

		public void ProcessFiles(string directory, string fileName) => Calls.Add($"ProcessFiles({directory},{fileName})");

		public void ProcessBatch(string directory, string batchName) => Calls.Add($"ProcessBatch({directory},{batchName})");

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName)
		{
			Calls.Add($"CompareFiles({directory},{fileName})");
			return new Dictionary<string, IReadOnlyCollection<string>>();
		}

		public void RunIterativeMerge(string directory, string fileName) => Calls.Add($"RunIterativeMerge({directory},{fileName})");

		public void ListBatches() => Calls.Add("ListBatches");

		public void StartInteractiveMode() => Calls.Add("StartInteractiveMode");
	}

	private RecordingApplicationService _applicationService = null!;

	[TestInitialize]
	public void Setup() => _applicationService = new RecordingApplicationService();

	/// <summary>
	/// Runs the handler with the console captured, so a test can assert on what was printed
	/// without the output landing in the test log.
	/// </summary>
	/// <param name="args">Command line arguments to process.</param>
	/// <returns>The handler's exit code and everything it wrote to standard output.</returns>
	private (int ExitCode, string Output) Run(params string[] args)
	{
		TextWriter originalOut = Console.Out;
		using StringWriter captured = new();
		Console.SetOut(captured);
		try
		{
			CommandLineHandler handler = new(_applicationService);
			int exitCode = handler.ProcessCommandLineArguments(args);
			return (exitCode, captured.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithHelpFlag_PrintsUsageAndDispatchesNothing()
	{
		(int exitCode, string output) = Run("-h");

		Assert.AreEqual(0, exitCode);
		Assert.IsTrue(output.Contains("BlastMerge - Cross-Repository File Synchronization Tool", StringComparison.Ordinal), output);
		Assert.IsTrue(output.Contains("Usage:", StringComparison.Ordinal), output);
		Assert.IsTrue(output.Contains("Examples:", StringComparison.Ordinal), output);
		Assert.IsEmpty(_applicationService.Calls);
	}

	/// <summary>
	/// The usage text has to name the command the tool actually installs as, not the assembly
	/// that happens to host it.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_WithHelpFlag_NamesTheInstalledCommand()
	{
		(_, string output) = Run("-h");

		Assert.IsTrue(output.Contains("blastmerge <directory> <filename>", StringComparison.Ordinal), output);
		Assert.IsFalse(output.Contains("BlastMerge.exe", StringComparison.Ordinal), output);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithVersionFlag_PrintsVersionAndDispatchesNothing()
	{
		(int exitCode, string output) = Run("-v");

		Assert.AreEqual(0, exitCode);
		Assert.IsTrue(output.StartsWith("BlastMerge v", StringComparison.Ordinal), output);
		Assert.IsEmpty(_applicationService.Calls);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithListBatchesFlag_ListsBatches()
	{
		(int exitCode, _) = Run("-l");

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(["ListBatches"], _applicationService.Calls);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithDirectoryAndFileName_ProcessesFiles()
	{
		(int exitCode, _) = Run("some-directory", "README.md");

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(["ProcessFiles(some-directory,README.md)"], _applicationService.Calls);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithDirectoryAndBatchName_ProcessesBatch()
	{
		(int exitCode, _) = Run("some-directory", "-b", "Config Files");

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(["ProcessBatch(some-directory,Config Files)"], _applicationService.Calls);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithNoArguments_StartsInteractiveMode()
	{
		(int exitCode, _) = Run();

		Assert.AreEqual(0, exitCode);
		Assert.AreSequenceEqual(["StartInteractiveMode"], _applicationService.Calls);
	}

	[TestMethod]
	public void ProcessCommandLineArguments_WithUnrecognisedOption_ReportsFailure()
	{
		(int exitCode, _) = Run("--not-an-option");

		Assert.AreEqual(1, exitCode);
		Assert.IsEmpty(_applicationService.Calls);
	}
}
