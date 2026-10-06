// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.CLI;
using ktsu.BlastMerge.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for how <see cref="CommandLineHandler"/> reports a failure raised by the operation it
/// dispatched to, complementing <see cref="CommandLineHandlerTests"/>, which covers dispatch itself.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CommandLineHandlerCoverageTests
{
	/// <summary>
	/// An application service whose every operation throws the exception it was given.
	/// </summary>
	/// <param name="exception">The exception to throw.</param>
	private sealed class ThrowingApplicationService(Exception exception) : IApplicationService
	{
		public void ProcessFiles(string directory, string fileName) => throw exception;

		public void ProcessBatch(string directory, string batchName) => throw exception;

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName) => throw exception;

		public void RunIterativeMerge(string directory, string fileName) => throw exception;

		public void ListBatches() => throw exception;

		public void StartInteractiveMode() => throw exception;
	}

	/// <summary>
	/// Creates the exception named by a test row.
	/// </summary>
	/// <param name="kind">The exception type's name.</param>
	/// <returns>An exception of that type carrying the message "boom".</returns>
	private static Exception CreateException(string kind) => kind switch
	{
		nameof(DirectoryNotFoundException) => new DirectoryNotFoundException("boom"),
		nameof(UnauthorizedAccessException) => new UnauthorizedAccessException("boom"),
		nameof(ArgumentException) => new ArgumentException("boom"),
		nameof(InvalidOperationException) => new InvalidOperationException("boom"),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown exception kind."),
	};

	/// <summary>
	/// Runs the handler with standard output captured.
	/// </summary>
	/// <param name="service">The application service to dispatch to.</param>
	/// <param name="args">The command line arguments.</param>
	/// <returns>The exit code and everything written to standard output.</returns>
	private static (int ExitCode, string Output) Run(IApplicationService service, params string[] args)
	{
		TextWriter originalOut = Console.Out;
		using StringWriter captured = new();
		Console.SetOut(captured);
		try
		{
			int exitCode = new CommandLineHandler(service).ProcessCommandLineArguments(args);
			return (exitCode, captured.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}

	/// <summary>
	/// Each kind of failure an operation can raise is reported with its own prefix and a failing exit code.
	/// </summary>
	/// <param name="kind">The exception type the operation throws.</param>
	/// <param name="expectedPrefix">The prefix the handler reports it with.</param>
	[TestMethod]
	[DataRow(nameof(DirectoryNotFoundException), "Directory not found: boom")]
	[DataRow(nameof(UnauthorizedAccessException), "Access denied: boom")]
	[DataRow(nameof(ArgumentException), "Invalid parameter: boom")]
	[DataRow(nameof(InvalidOperationException), "Command execution error: boom")]
	public void ProcessCommandLineArguments_OperationThrows_ReportsTheFailure(string kind, string expectedPrefix)
	{
		ThrowingApplicationService service = new(CreateException(kind));

		(int exitCode, string output) = Run(service, "some-directory", "file.txt");

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(output, expectedPrefix);
	}

	/// <summary>
	/// A failure while listing batches is reported the same way as any other operation's.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_ListBatchesThrows_ReportsTheFailure()
	{
		ThrowingApplicationService service = new(new UnauthorizedAccessException("locked"));

		(int exitCode, string output) = Run(service, "-l");

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(output, "Access denied: locked");
	}

	/// <summary>
	/// A failure while running a batch is reported rather than escaping.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_ProcessBatchThrows_ReportsTheFailure()
	{
		ThrowingApplicationService service = new(new DirectoryNotFoundException("gone"));

		(int exitCode, string output) = Run(service, "some-directory", "-b", "Docs");

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(output, "Directory not found: gone");
	}

	/// <summary>
	/// A failure from interactive mode is reported rather than escaping.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_InteractiveModeThrows_ReportsTheFailure()
	{
		ThrowingApplicationService service = new(new InvalidOperationException("no console"));

		(int exitCode, string output) = Run(service);

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(output, "Command execution error: no console");
	}

	/// <summary>
	/// An exception outside the reported set is not swallowed.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_UnexpectedException_Propagates()
	{
		ThrowingApplicationService service = new(new NotSupportedException("unexpected"));

		Assert.ThrowsExactly<NotSupportedException>(() => Run(service, "-l"));
	}

	/// <summary>
	/// A null argument array is refused outright.
	/// </summary>
	[TestMethod]
	public void ProcessCommandLineArguments_NullArguments_Throws()
	{
		CommandLineHandler handler = new(new ThrowingApplicationService(new InvalidOperationException()));

		Assert.ThrowsExactly<ArgumentNullException>(() => handler.ProcessCommandLineArguments(null!));
	}
}
