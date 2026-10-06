// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Text;
using ktsu.BlastMerge.Cli;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="Program.Main"/>, run in process against the real application service with
/// standard output captured and the Spectre console replaced by a test console.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ProgramTests : ConsoleTestBase
{
	/// <summary>
	/// Empties the saved batches, the recent batch and the input history, so each test starts from a known state.
	/// </summary>
	[TestInitialize]
	public void ResetSharedAppData() => ConsoleApplicationServiceTests.ResetAppData();

	/// <summary>
	/// Runs <see cref="Program.Main"/> with standard output and standard error captured, restoring the
	/// console encodings it changes afterwards.
	/// </summary>
	/// <param name="args">The command line arguments.</param>
	/// <returns>The exit code, and everything written to standard output and standard error.</returns>
	private static (int ExitCode, string Output, string Error) RunMain(params string[] args)
	{
		TextWriter originalOut = System.Console.Out;
		TextWriter originalError = System.Console.Error;
		Encoding originalOutputEncoding = System.Console.OutputEncoding;
		Encoding originalInputEncoding = System.Console.InputEncoding;
		using StringWriter output = new();
		using StringWriter error = new();
		System.Console.SetOut(output);
		System.Console.SetError(error);
		try
		{
			int exitCode = Program.Main(args);
			return (exitCode, output.ToString(), error.ToString());
		}
		catch (IOException ex)
		{
			// Setting the console encodings needs a console; a host without one cannot run Main at all.
			Assert.Inconclusive($"Program.Main could not set the console encoding in this host: {ex.Message}");
			throw;
		}
		finally
		{
			System.Console.SetOut(originalOut);
			System.Console.SetError(originalError);
			RestoreEncodings(originalOutputEncoding, originalInputEncoding);
		}
	}

	/// <summary>
	/// Puts back the console encodings <see cref="Program.Main"/> replaces.
	/// </summary>
	/// <param name="outputEncoding">The original output encoding.</param>
	/// <param name="inputEncoding">The original input encoding.</param>
	private static void RestoreEncodings(Encoding outputEncoding, Encoding inputEncoding)
	{
		try
		{
			System.Console.OutputEncoding = outputEncoding;
			System.Console.InputEncoding = inputEncoding;
		}
		catch (IOException)
		{
			// Nothing was changed if the host refused the encodings in the first place.
		}
	}

	/// <summary>
	/// The version flag prints the version and succeeds.
	/// </summary>
	[TestMethod]
	public void Main_WithVersionFlag_PrintsTheVersion()
	{
		(int exitCode, string output, _) = RunMain("-v");

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(output, "BlastMerge v");
	}

	/// <summary>
	/// The help flag prints the usage and succeeds.
	/// </summary>
	[TestMethod]
	public void Main_WithHelpFlag_PrintsUsage()
	{
		(int exitCode, string output, _) = RunMain("-h");

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(output, "Usage:");
	}

	/// <summary>
	/// An unrecognised option fails with the parser's explanation on standard error.
	/// </summary>
	[TestMethod]
	public void Main_WithUnknownOption_Fails()
	{
		(int exitCode, _, string error) = RunMain("--no-such-option");

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(error, "no-such-option");
	}

	/// <summary>
	/// Listing batches goes through the real application service.
	/// </summary>
	[TestMethod]
	public void Main_WithListFlag_ListsSavedBatches()
	{
		BatchConfiguration batch = new()
		{
			Name = "From Main",
			FilePatterns = ["README.md"],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));

		(int exitCode, string output, _) = RunMain("-l");

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(output, "  - From Main");
	}

	/// <summary>
	/// Processing files in a directory that does not exist fails with an explanation.
	/// </summary>
	[TestMethod]
	public void Main_WithMissingDirectory_Fails()
	{
		string missing = Path.Combine(TempDirectory, "missing");

		(int exitCode, string output, _) = RunMain(missing, "README.md");

		Assert.AreEqual(1, exitCode);
		StringAssert.Contains(output, "Directory not found:");
	}

	/// <summary>
	/// Processing files in an existing directory runs the file workflow.
	/// </summary>
	[TestMethod]
	public void Main_WithDirectoryAndFileName_ProcessesTheFiles()
	{
		WriteFile("README.md", "content");
		SelectIndex(4);

		(int exitCode, _, _) = RunMain(TempDirectory, "README.md");

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(Output, "Found 1 files in 1 groups:");
	}

	/// <summary>
	/// Running an unknown batch reports it through the console and still exits successfully.
	/// </summary>
	[TestMethod]
	public void Main_WithUnknownBatch_ReportsItAndSucceeds()
	{
		(int exitCode, _, _) = RunMain(TempDirectory, "-b", "Nope");

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(Output, "Batch configuration 'Nope' not found.");
	}

	/// <summary>
	/// With no arguments Main starts interactive mode, which returns when the user exits.
	/// </summary>
	[TestMethod]
	public void Main_WithNoArguments_RunsInteractiveMode()
	{
		SelectIndex(7);

		(int exitCode, _, _) = RunMain();

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(Output, "Main Menu");
	}
}
