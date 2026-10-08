// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the paths of <see cref="BatchProcessor"/> the existing suite leaves untouched: patterns the
/// user skips, a missing directory or failing progress report in the discrete-phase pipeline, how a
/// discovered file is attributed to a pattern, and the callbacks overload of the single-pattern entry point.
/// </summary>
[TestClass]
public class BatchProcessorCoverageTests : MockFileSystemTestBase
{
	private static readonly Func<string, string, string?, MergeResult?> NoMerge = (_, _, _) => null;
	private static readonly Action<MergeSessionStatus> IgnoreStatus = _ => { };
	private static readonly Func<bool> KeepGoing = () => true;

	/// <summary>
	/// A pattern the user declines when prompted is recorded as skipped and is not searched.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_UserSkipsPattern_RecordsSkip()
	{
		CreateFile(Path.Join("a", "same.txt"), "one");
		CreateFile(Path.Join("b", "same.txt"), "two");
		BatchConfiguration batch = new()
		{
			Name = "prompting",
			FilePatterns = ["same.txt"],
			PromptBeforeEachPattern = true,
		};
		List<string> asked = [];
		int merges = 0;

		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			TestDirectory,
			(_, _, _) =>
			{
				merges++;
				return null;
			},
			IgnoreStatus,
			KeepGoing,
			pattern =>
			{
				asked.Add(pattern);
				return false;
			},
			MockFileSystem);

		Assert.IsTrue(result.Success);
		Assert.AreSequenceEqual(["same.txt"], asked);
		Assert.AreEqual(0, merges);
		Assert.HasCount(1, result.PatternResults);
		PatternResult skipped = result.PatternResults[0];
		Assert.AreEqual("same.txt", skipped.Pattern);
		Assert.IsTrue(skipped.Success);
		Assert.AreEqual("Skipped by user", skipped.Message);
		Assert.AreEqual(0, skipped.FilesFound);
		Assert.AreEqual("Batch completed successfully. Processed 1/1 patterns.", result.Summary);
	}

	/// <summary>
	/// The discrete-phase pipeline refuses a directory that does not exist.
	/// </summary>
	[TestMethod]
	public void ProcessBatchWithDiscretePhases_MissingDirectory_Fails()
	{
		string missing = Path.Join(TestDirectory, "missing");
		BatchConfiguration batch = new() { Name = "absent", FilePatterns = ["*.txt"] };

		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(batch, missing, NoMerge, IgnoreStatus, KeepGoing, fileSystem: MockFileSystem);

		Assert.IsFalse(result.Success);
		Assert.AreEqual($"Directory does not exist: {missing}", result.Summary);
		Assert.AreEqual("absent", result.BatchName);
		Assert.IsEmpty(result.PatternResults);
	}

	/// <summary>
	/// An I/O failure during the run is reported in the summary rather than thrown.
	/// </summary>
	[TestMethod]
	public void ProcessBatchWithDiscretePhases_IOFailure_IsReportedInSummary()
	{
		CreateFile("only.txt", "x");
		BatchConfiguration batch = new() { Name = "failing", FilePatterns = ["*.txt"] };

		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			TestDirectory,
			NoMerge,
			IgnoreStatus,
			KeepGoing,
			_ => throw new IOException("progress sink is gone"),
			fileSystem: MockFileSystem);

		Assert.IsFalse(result.Success);
		Assert.AreEqual("Error during batch processing: progress sink is gone", result.Summary);
	}

	/// <summary>
	/// A file found by a wildcard pattern that is neither "*" nor "*.ext" is attributed to that pattern.
	/// </summary>
	[TestMethod]
	public void ProcessBatchWithDiscretePhases_QuestionMarkPattern_AttributesFileToPattern()
	{
		string root = CreateDirectory("question");
		CreateFile(Path.Join("question", "ab.txt"), "x");
		BatchConfiguration batch = new() { Name = "question", FilePatterns = ["a?.txt"] };

		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(batch, root, NoMerge, IgnoreStatus, KeepGoing, fileSystem: MockFileSystem);

		Assert.IsTrue(result.Success);
		Assert.HasCount(1, result.PatternResults);
		Assert.AreEqual("a?.txt", result.PatternResults[0].Pattern);
		Assert.AreEqual("ab.txt", result.PatternResults[0].FileName);
		Assert.AreEqual("Only one file found, no action needed", result.PatternResults[0].Message);
	}

	/// <summary>
	/// A file found by the "*" pattern is attributed to that pattern.
	/// </summary>
	[TestMethod]
	public void ProcessBatchWithDiscretePhases_StarPattern_AttributesFileToStar()
	{
		string root = CreateDirectory("star");
		CreateFile(Path.Join("star", "notes.md"), "x");
		BatchConfiguration batch = new() { Name = "star", FilePatterns = ["*"] };

		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(batch, root, NoMerge, IgnoreStatus, KeepGoing, fileSystem: MockFileSystem);

		Assert.IsTrue(result.Success);
		Assert.HasCount(1, result.PatternResults);
		Assert.AreEqual("*", result.PatternResults[0].Pattern);
		Assert.AreEqual("notes.md", result.PatternResults[0].FileName);
	}

	/// <summary>
	/// The callbacks overload refuses a missing set of callbacks.
	/// </summary>
	[TestMethod]
	public void ProcessSinglePatternWithPaths_NullCallbacks_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessSinglePatternWithPaths("*.txt", [], TestDirectory, [], null!, MockFileSystem));
	}

	/// <summary>
	/// The callbacks overload merges differing files that share a name.
	/// </summary>
	[TestMethod]
	public void ProcessSinglePatternWithPaths_WithCallbacks_MergesFiles()
	{
		string file1 = CreateFile(Path.Join("left", "same.txt"), "a\nb\n");
		string file2 = CreateFile(Path.Join("right", "same.txt"), "a\nc\n");
		List<string> progress = [];
		ProcessingCallbacks callbacks = new(
			(_, _, _) => new MergeResult(["a", "merged"], []),
			IgnoreStatus,
			KeepGoing,
			progress.Add);

		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths("same.txt", [], TestDirectory, [], callbacks, MockFileSystem);

		Assert.IsTrue(result.Success);
		Assert.AreEqual("same.txt", result.Pattern);
		Assert.AreEqual(2, result.FilesFound);
		Assert.AreEqual(MockFileSystem.File.ReadAllText(file1), MockFileSystem.File.ReadAllText(file2));
		Assert.Contains("merged", MockFileSystem.File.ReadAllText(file1));
	}
}
