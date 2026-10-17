// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that pass a file system explicitly, without installing it in <see cref="FileSystemProvider"/>,
/// so a call that drops the argument and falls back to the global provider reads the real disk and fails.
/// </summary>
[TestClass]
public class ExplicitFileSystemTests
{
	private MockFileSystem mockFileSystem = null!;
	private string testRoot = null!;

	[TestInitialize]
	public void SetUp()
	{
		FileSystemProvider.ResetToDefault();
		mockFileSystem = new MockFileSystem();
		testRoot = Path.Combine(Path.GetTempPath(), $"blastmerge-explicit-fs-{Guid.NewGuid():N}");
	}

	[TestCleanup]
	public void TearDown() => FileSystemProvider.ResetToDefault();

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithExplicitFileSystem_MergesThroughIt()
	{
		// Arrange
		string file1 = Path.Combine(testRoot, "one", "b.cfg");
		string file2 = Path.Combine(testRoot, "two", "b.cfg");
		mockFileSystem.AddFile(file1, new("first\n"));
		mockFileSystem.AddFile(file2, new("second\n"));

		BatchConfiguration batch = new()
		{
			Name = "Explicit File System",
			FilePatterns = ["*.cfg"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testRoot,
			(path1, path2, output) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			null,
			0,
			mockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, $"Batch should merge through the supplied file system. Summary: {result.Summary}; " +
			string.Join("; ", result.PatternResults.Select(r => r.Message)));
		StringAssert.Contains(mockFileSystem.File.ReadAllText(file1), "merged");
		StringAssert.Contains(mockFileSystem.File.ReadAllText(file2), "merged");
	}

	[TestMethod]
	public void FindDifferences_WithExplicitFileSystem_ClassifiesFilesThroughIt()
	{
		// Arrange
		string dir1 = Path.Combine(testRoot, "left");
		string dir2 = Path.Combine(testRoot, "right");
		mockFileSystem.AddFile(Path.Combine(dir1, "same.txt"), new("same\n"));
		mockFileSystem.AddFile(Path.Combine(dir2, "same.txt"), new("same\n"));
		mockFileSystem.AddFile(Path.Combine(dir1, "changed.txt"), new("left\n"));
		mockFileSystem.AddFile(Path.Combine(dir2, "changed.txt"), new("right\n"));

		// Act
		DirectoryComparisonResult result = FileDiffer.FindDifferences(dir1, dir2, "*.txt", false, mockFileSystem);

		// Assert
		CollectionAssert.AreEqual(new[] { "same.txt" }, result.SameFiles.ToArray());
		CollectionAssert.AreEqual(new[] { "changed.txt" }, result.ModifiedFiles.ToArray());
	}
}
