// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Text;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins the UTF-8 byte order mark of merged output across every merge entry point.
/// </summary>
/// <remarks>
/// The sources are read as text, which drops the byte order mark, and merged content used to be
/// written back without one. Visual Studio saves C# files with a byte order mark by default, so
/// merging them across repositories put a whole-file change in every repository's diff.
/// </remarks>
[TestClass]
public class MergeEncodingTests : MockFileSystemTestBase
{
	private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

	private static readonly UTF8Encoding WithBom = new(encoderShouldEmitUTF8Identifier: true);
	private static readonly UTF8Encoding WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// Resolves every block by taking the second version.
	/// </summary>
	private static BlockChoice TakeVersion2(DiffPlex.Model.DiffBlock block, BlockContext context, int blockNumber) =>
		BlockChoice.UseVersion2;

	private string CreateEncodedFile(string relativePath, string content, Encoding encoding)
	{
		string path = CreateFile(relativePath, string.Empty);
		MockFileSystem.File.WriteAllBytes(path, [.. encoding.GetPreamble(), .. encoding.GetBytes(content)]);
		return path;
	}

	private bool StartsWithBom(string path)
	{
		byte[] bytes = MockFileSystem.File.ReadAllBytes(path);
		return bytes.Length >= Utf8Bom.Length && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithBomSources_KeepsTheBom()
	{
		// Arrange
		string file1 = CreateEncodedFile("dir1/x.cs", "line1\nline2\n", WithBom);
		string file2 = CreateEncodedFile("dir2/x.cs", "line1\nline2 changed\n", WithBom);
		string untouched = CreateEncodedFile("dir3/x.cs", "line1\nline2 changed\n", WithBom);

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = "hash1" },
			new([file2]) { Hash = "hash2" },
		];

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeVersion2, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "The merge should complete");
		Assert.IsTrue(StartsWithBom(file1), "The merged file should keep the byte order mark its source had");
		Assert.IsTrue(StartsWithBom(file2), "The merged file should keep the byte order mark its source had");
		CollectionAssert.AreEqual(MockFileSystem.File.ReadAllBytes(untouched), MockFileSystem.File.ReadAllBytes(file1),
			"The merged file should be byte-identical to an untouched copy of the same text");
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithoutBomSources_DoesNotAddOne()
	{
		// Arrange
		string file1 = CreateEncodedFile("dir1/x.cs", "line1\nline2\n", WithoutBom);
		string file2 = CreateEncodedFile("dir2/x.cs", "line1\nline2 changed\n", WithoutBom);

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = "hash1" },
			new([file2]) { Hash = "hash2" },
		];

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeVersion2, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "The merge should complete");
		Assert.IsFalse(StartsWithBom(file1), "A byte order mark should not be added to sources that had none");
		Assert.IsFalse(StartsWithBom(file2), "A byte order mark should not be added to sources that had none");
	}

	[TestMethod]
	public void ProcessSinglePattern_WithBomSources_KeepsTheBom()
	{
		// Arrange
		string testDir = MockFileSystem.Path.Combine(TestDirectory, "batch");
		MockFileSystem.Directory.CreateDirectory(testDir);
		string pathA = CreateEncodedFile("batch/repo-a/x.cs", "line1\nline2\n", WithBom);
		string pathB = CreateEncodedFile("batch/repo-b/x.cs", "line1\nline2 changed\n", WithBom);

		// Act - the batch path reaches the same merge through its own writer
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"x.cs",
			[],
			testDir,
			[],
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, TakeVersion2, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing two differing versions should succeed");
		Assert.IsTrue(StartsWithBom(pathA), "The merged file should keep the byte order mark its source had");
		Assert.IsTrue(StartsWithBom(pathB), "The merged file should keep the byte order mark its source had");
	}

	[TestMethod]
	public void MergeFiles_WithOneBomSource_ReportsTheBom()
	{
		// Arrange
		string file1 = CreateEncodedFile("dir1/x.cs", "shared\nonly-in-a\n", WithoutBom);
		string file2 = CreateEncodedFile("dir2/x.cs", "shared\nonly-in-b\n", WithBom);

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		Assert.IsTrue(result.HasUtf8Bom, "The result should remember that a source carried a byte order mark");
		CollectionAssert.AreEqual(Utf8Bom, result.ContentEncoding.GetPreamble(), "The result should be written with the byte order mark");
	}

	[TestMethod]
	public void MergeFiles_WithoutBomSources_ReportsNoBom()
	{
		// Arrange
		string file1 = CreateEncodedFile("dir1/x.cs", "shared\nonly-in-a\n", WithoutBom);
		string file2 = CreateEncodedFile("dir2/x.cs", "shared\nonly-in-b\n", WithoutBom);

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		Assert.IsFalse(result.HasUtf8Bom, "Neither source carried a byte order mark");
		Assert.IsEmpty(result.ContentEncoding.GetPreamble(), "The result should be written without a byte order mark");
	}
}
