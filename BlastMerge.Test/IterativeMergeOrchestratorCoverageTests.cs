// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers how <see cref="IterativeMergeOrchestrator.StartIterativeMergeProcess"/> ends a session early:
/// when writing the merged content fails, and when the user declines to continue.
/// </summary>
[TestClass]
public class IterativeMergeOrchestratorCoverageTests : MockFileSystemTestBase
{
	/// <summary>
	/// A write that fails with an I/O error stops the session and reports the error with the merged content.
	/// </summary>
	[TestMethod]
	public void StartIterativeMergeProcess_WriteFailsWithIOError_ReportsError()
	{
		string file1 = CreateFile(Path.Combine("one", "same.txt"), "a\nb\nc\n");
		string file2 = CreateFile(Path.Combine("two", "same.txt"), "a\nb\nd\n");
		List<FileGroup> groups = [new FileGroup([file1]) { Hash = "h1" }, new FileGroup([file2]) { Hash = "h2" }];

		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			groups,
			(_, _, _) =>
			{
				MockFileSystem.Directory.Delete(Path.Combine(TestDirectory, "one"), true);
				return new MergeResult(["a", "b", "merged"], []);
			},
			_ => { },
			() => true,
			MockFileSystem);

		Assert.IsFalse(result.IsSuccessful);
		Assert.StartsWith("error: ", result.OriginalFileName);
		Assert.IsNotNull(result.FinalMergedContent);
		Assert.Contains("merged", result.FinalMergedContent);
		Assert.AreEqual(0, result.TotalMergeOperations);
		Assert.AreEqual(2, result.InitialFileGroups);
		Assert.HasCount(1, result.Operations);
	}

	/// <summary>
	/// A write refused for lack of access stops the session and says access was denied.
	/// </summary>
	[TestMethod]
	public void StartIterativeMergeProcess_WriteDenied_ReportsAccessDenied()
	{
		string file1 = CreateFile(Path.Combine("one", "same.txt"), "a\nb\nc\n");
		string file2 = CreateFile(Path.Combine("two", "same.txt"), "a\nb\nd\n");
		List<FileGroup> groups = [new FileGroup([file1]) { Hash = "h1" }, new FileGroup([file2]) { Hash = "h2" }];

		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			groups,
			(_, _, _) =>
			{
				MockFileSystem.File.SetAttributes(file1, FileAttributes.ReadOnly);
				MockFileSystem.File.SetAttributes(file2, FileAttributes.ReadOnly);
				return new MergeResult(["a", "b", "merged"], []);
			},
			_ => { },
			() => true,
			MockFileSystem);

		Assert.IsFalse(result.IsSuccessful);
		Assert.StartsWith("access denied: ", result.OriginalFileName);
		Assert.AreEqual(0, result.TotalMergeOperations);
	}

	/// <summary>
	/// Declining to continue after the first merge leaves the session incomplete, carrying what was merged so far.
	/// </summary>
	[TestMethod]
	public void StartIterativeMergeProcess_UserDeclinesToContinue_IsIncomplete()
	{
		string file1 = CreateFile(Path.Combine("one", "same.txt"), "a\nb\nc\nd\n");
		string file2 = CreateFile(Path.Combine("two", "same.txt"), "a\nb\nc\nx\n");
		string file3 = CreateFile(Path.Combine("three", "same.txt"), "q\nr\ns\nt\n");
		List<FileGroup> groups =
		[
			new FileGroup([file1]) { Hash = "h1" },
			new FileGroup([file2]) { Hash = "h2" },
			new FileGroup([file3]) { Hash = "h3" },
		];
		int continuationAsks = 0;
		List<MergeSessionStatus> statuses = [];

		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			groups,
			(_, _, _) => new MergeResult(["a", "b", "c", "merged"], []),
			statuses.Add,
			() =>
			{
				continuationAsks++;
				return false;
			},
			MockFileSystem);

		Assert.IsFalse(result.IsSuccessful);
		Assert.AreEqual("incomplete", result.OriginalFileName);
		Assert.AreEqual(1, result.TotalMergeOperations);
		Assert.AreEqual(3, result.InitialFileGroups);
		Assert.AreEqual(3, result.TotalFilesMerged);
		Assert.AreEqual(1, continuationAsks);
		Assert.HasCount(1, statuses);
		Assert.HasCount(1, result.Operations);
		Assert.IsNotNull(result.FinalMergedContent);
		Assert.Contains("merged", result.FinalMergedContent);
		Assert.AreEqual(result.FinalMergedContent, MockFileSystem.File.ReadAllText(file1));
		Assert.AreEqual(result.FinalMergedContent, MockFileSystem.File.ReadAllText(file2));
		Assert.AreEqual("q\nr\ns\nt\n", MockFileSystem.File.ReadAllText(file3));
	}
}
