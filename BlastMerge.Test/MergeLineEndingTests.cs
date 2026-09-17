// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins the line-ending style of merged output across every merge entry point.
/// </summary>
/// <remarks>
/// Each entry point used to resolve the style for itself — <see cref="Environment.NewLine"/> in the
/// core services, a hardcoded line feed in the console app's interactive merge — so the style of a
/// merged file depended on which command produced it and which OS it ran on. A CRLF file merged on
/// Linux came back entirely LF, which turns a small reviewable merge into a full-file rewrite in
/// <c>git diff</c>. These tests assert the source's style survives, and they are written to be
/// platform-independent so they fail on the host that introduced the regression rather than only on
/// the other one.
/// </remarks>
[TestClass]
public class MergeLineEndingTests : MockFileSystemTestBase
{
	private const string Crlf = "\r\n";
	private const string Lf = "\n";

	/// <summary>
	/// Merges every block by taking both sides, so no line that reaches the merge is dropped.
	/// </summary>
	private static BlockChoice TakeBothSides(DiffPlex.Model.DiffBlock block, BlockContext context, int blockNumber) =>
		BlockChoice.UseBoth;

	[TestMethod]
	public void MergeFiles_WithCrlfSources_ReportsCrlfAsTheResultLineEnding()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Crlf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Crlf}only-in-b");

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		Assert.AreEqual(Crlf, result.LineEnding, "A merge of CRLF sources should report CRLF");
		StringAssert.Contains(result.ToContent(), Crlf, "The rendered content should carry CRLF");
	}

	[TestMethod]
	public void MergeFiles_WithLfSources_ReportsLfAsTheResultLineEnding()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Lf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Lf}only-in-b");

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		Assert.AreEqual(Lf, result.LineEnding, "A merge of LF sources should report LF");
		Assert.IsFalse(
			result.ToContent().Contains('\r', StringComparison.Ordinal),
			"An LF merge must not gain carriage returns, including when it runs on Windows");
	}

	[TestMethod]
	public void MergeFiles_WithCrlfSources_DoesNotLeaveCarriageReturnsOnTheMergedLines()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Crlf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Crlf}only-in-b");

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert - a carriage return inside a line would be re-emitted next to the joiner
		Assert.IsFalse(
			result.MergedLines.Any(line => line.Contains('\r', StringComparison.Ordinal)),
			"Splitting CRLF content must not leave a stray carriage return on each line");
	}

	[TestMethod]
	public void PerformMergeWithConflictResolution_WithCrlfSources_ReportsCrlfAsTheResultLineEnding()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Crlf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Crlf}only-in-b");

		// Act
		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1, file2, null, TakeBothSides, MockFileSystem);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(Crlf, result.LineEnding, "The block-selection merge path should preserve CRLF too");
		Assert.IsFalse(
			result.MergedLines.Any(line => line.Contains('\r', StringComparison.Ordinal)),
			"Splitting CRLF content must not leave a stray carriage return on each line");
	}

	[TestMethod]
	public void PerformMergeWithConflictResolution_WithCrlfExistingContent_ReportsCrlfAsTheResultLineEnding()
	{
		// Arrange - the chained case, where the first side is a previous merge's output
		string file2 = CreateFile("dir2/app.config", $"shared{Crlf}only-in-b");
		string existingMergedContent = $"shared{Crlf}only-in-a";

		// Act
		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			"unused.config", file2, existingMergedContent, TakeBothSides, MockFileSystem);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(Crlf, result.LineEnding, "Chaining a merge should not lose the style either");
		Assert.IsFalse(
			result.MergedLines.Any(line => line.Contains('\r', StringComparison.Ordinal)),
			"Existing merged content was previously split on the platform newline, stranding carriage returns on Linux");
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithCrlfSources_WritesCrlfToEveryFile()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Crlf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Crlf}only-in-b");

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = "hash1" },
			new([file2]) { Hash = "hash2" },
		];

		// Act - the production merge callback, which is what the console app wires up
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeBothSides, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "The merge should complete");
		AssertFileUsesOnly(file1, Crlf);
		AssertFileUsesOnly(file2, Crlf);
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithLfSources_WritesLfToEveryFile()
	{
		// Arrange
		string file1 = CreateFile("dir1/app.config", $"shared{Lf}only-in-a");
		string file2 = CreateFile("dir2/app.config", $"shared{Lf}only-in-b");

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = "hash1" },
			new([file2]) { Hash = "hash2" },
		];

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeBothSides, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "The merge should complete");
		AssertFileUsesOnly(file1, Lf);
		AssertFileUsesOnly(file2, Lf);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithCrlfSources_WritesCrlfToEveryFile()
	{
		// Arrange
		string testDir = Path.Combine(TestDirectory, "batch");
		MockFileSystem.Directory.CreateDirectory(testDir);
		string pathA = CreateFile("batch/repo-a/app.config", $"shared{Crlf}only-in-a");
		string pathB = CreateFile("batch/repo-b/app.config", $"shared{Crlf}only-in-b");

		// Act - the batch path reaches the same merge through its own writer
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"app.config",
			[],
			testDir,
			[],
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeBothSides, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing two differing versions should succeed");
		AssertFileUsesOnly(pathA, Crlf);
		AssertFileUsesOnly(pathB, Crlf);
	}

	/// <summary>
	/// Asserts the file's content is written entirely in the expected line-ending style.
	/// </summary>
	/// <param name="path">The file to inspect.</param>
	/// <param name="expected">The only line ending the file should contain.</param>
	private void AssertFileUsesOnly(string path, string expected)
	{
		string content = MockFileSystem.File.ReadAllText(path);

		StringAssert.Contains(
			content,
			expected,
			$"{path} should be written with the line ending its sources used");

		Assert.AreEqual(
			expected,
			LineEndingDetector.Detect(content),
			$"{path} should contain no line ending other than the one its sources used");

		if (expected == Lf)
		{
			Assert.IsFalse(
				content.Contains('\r', StringComparison.Ordinal),
				$"{path} should carry no carriage returns at all");
		}
	}
}
