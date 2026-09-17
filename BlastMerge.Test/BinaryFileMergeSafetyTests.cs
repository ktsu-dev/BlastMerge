// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that binary content never reaches the text diff and merge pipeline.
/// </summary>
/// <remarks>
/// A glob that matches an icon or a serialized asset used to walk straight into the merge pipeline:
/// the two most similar versions were decoded as UTF-8, conflict markers were inserted into the byte
/// stream, and the result was written back with <c>WriteAllText</c>. Every byte that was not valid
/// UTF-8 had already been replaced with U+FFFD by then, so the original content was gone and nothing
/// warned the user. The fixtures below come in two kinds: bytes that differ from each other but
/// decode to the same replacement text, which a text-level comparison cannot tell apart, and bytes
/// that decode cleanly yet are still binary, which nothing on the read path objects to at all.
/// </remarks>
[TestClass]
public class BinaryFileMergeSafetyTests : MockFileSystemTestBase
{
	// Both sequences decode to "A�\n" as UTF-8, so any comparison that reads them as text
	// reports them as identical while their bytes differ.
	private static readonly byte[] BinaryVersion1 = [0x41, 0xFF, 0x0A];
	private static readonly byte[] BinaryVersion2 = [0x41, 0xFE, 0x0A];

	// These two decode without complaint — every byte is valid UTF-8 — so nothing along the read path
	// objects to them. They are still binary, and a text merge rewrites them with conflict markers.
	private static readonly byte[] NulByteVersion1 = [0x00, 0x01, 0x41, 0x0A];
	private static readonly byte[] NulByteVersion2 = [0x00, 0x01, 0x42, 0x0A];

	[TestMethod]
	public void CalculateFileSimilarity_WithDivergentBinaryFiles_ReportsThemAsDifferent()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		// Act
		double similarity = FileDiffer.CalculateFileSimilarity(file1, file2, MockFileSystem);

		// Assert - decoded as text these two are indistinguishable, so a text comparison scores 1.0
		Assert.AreEqual(0.0, similarity, "Binary files with different bytes should not be reported as similar");
	}

	[TestMethod]
	public void CalculateFileSimilarity_WithIdenticalBinaryFiles_ReportsThemAsIdentical()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion1);

		// Act
		double similarity = FileDiffer.CalculateFileSimilarity(file1, file2, MockFileSystem);

		// Assert - same or different is all the tool claims about binary content, and this is same
		Assert.AreEqual(1.0, similarity, "Binary files with the same bytes should be reported as identical");
	}

	[TestMethod]
	public void AreFilesIdentical_WithDivergentBinaryFiles_ReturnsFalse()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		// Act
		bool identical = DiffPlexDiffer.AreFilesIdentical(file1, file2);

		// Assert - a UTF-8 read collapses both files onto the same replacement character
		Assert.IsFalse(identical, "Binary files with different bytes should not be reported as identical");
	}

	[TestMethod]
	public void GenerateUnifiedDiff_WithBinaryFiles_DoesNotEmitDecodedContent()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		// Act
		string diff = DiffPlexDiffer.GenerateUnifiedDiff(file1, file2);

		// Assert - the diff reports that the content differs without inventing lines from the bytes
		Assert.IsTrue(diff.Contains("Binary content"), "Binary files should be reported as binary rather than diffed line by line");
		Assert.IsFalse(diff.Contains('�'), "A diff of binary files should never show decoded bytes");
	}

	[TestMethod]
	public void FindMostSimilarFiles_WithBinaryVersions_OffersNoPair()
	{
		// Arrange - same filename, different content: the pair the merge pipeline would pick
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = FileHasher.ComputeFileHash(file1, MockFileSystem) },
			new([file2]) { Hash = FileHasher.ComputeFileHash(file2, MockFileSystem) }
		];

		// Act
		FileSimilarity? similarity = FileDiffer.FindMostSimilarFiles(fileGroups, MockFileSystem);

		// Assert
		Assert.IsNull(similarity, "Binary versions should never be offered as a merge candidate");
	}

	[TestMethod]
	public void MergeFiles_WithBinaryFiles_ThrowsInsteadOfMerging()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		// Act & Assert
		Assert.ThrowsExactly<BinaryContentException>(() => FileDiffer.MergeFiles(file1, file2, MockFileSystem));
	}

	[TestMethod]
	public void PerformMergeWithConflictResolution_WithBinaryFiles_ThrowsInsteadOfMerging()
	{
		// Arrange
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		// Act & Assert
		Assert.ThrowsExactly<BinaryContentException>(() => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			null,
			(block, context, blockNumber) => BlockChoice.UseVersion1,
			MockFileSystem));
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithBinaryVersions_LeavesEveryByteUntouched()
	{
		// Arrange - the scenario from the issue: two divergent copies of the same binary file, found
		// under different directories by one pattern
		string file1 = CreateBinaryFile("dir1/asset.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("dir2/asset.dat", BinaryVersion2);

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = FileHasher.ComputeFileHash(file1, MockFileSystem) },
			new([file2]) { Hash = FileHasher.ComputeFileHash(file2, MockFileSystem) }
		];

		bool mergeCallbackCalled = false;

		MergeResult? MergeCallback(string first, string second, string? existing)
		{
			mergeCallbackCalled = true;
			return FileDiffer.MergeFiles(first, second, MockFileSystem);
		}

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			MergeCallback,
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsFalse(mergeCallbackCalled, "Binary versions should never be handed to the merge callback");
		Assert.IsTrue(result.IsSuccessful, "Preserving binary files is a successful outcome, not a failure");
		CollectionAssert.AreEqual(BinaryVersion1, MockFileSystem.File.ReadAllBytes(file1), "The first file's bytes should be untouched");
		CollectionAssert.AreEqual(BinaryVersion2, MockFileSystem.File.ReadAllBytes(file2), "The second file's bytes should be untouched");
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithNulByteVersions_DoesNotRewriteThemWithConflictMarkers()
	{
		// Arrange - content that decodes cleanly as UTF-8 but is plainly not text. Nothing on the read
		// path objects to it, so before binary detection this pair merged without a murmur and each
		// file was rewritten with ASCII conflict markers embedded in its byte stream.
		string file1 = CreateBinaryFile("dir1/asset.bin", NulByteVersion1);
		string file2 = CreateBinaryFile("dir2/asset.bin", NulByteVersion2);

		List<FileGroup> fileGroups =
		[
			new([file1]) { Hash = FileHasher.ComputeFileHash(file1, MockFileSystem) },
			new([file2]) { Hash = FileHasher.ComputeFileHash(file2, MockFileSystem) }
		];

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			(first, second, existing) => FileDiffer.MergeFiles(first, second, MockFileSystem),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "Preserving binary files is a successful outcome, not a failure");
		CollectionAssert.AreEqual(NulByteVersion1, MockFileSystem.File.ReadAllBytes(file1), "The first file's bytes should be untouched");
		CollectionAssert.AreEqual(NulByteVersion2, MockFileSystem.File.ReadAllBytes(file2), "The second file's bytes should be untouched");
	}

	[TestMethod]
	public void ProcessSinglePattern_WithBinaryFiles_SkipsThemInsteadOfMerging()
	{
		// Arrange - a pattern broad enough to match binary files, which the batch path used to merge
		// pairwise without ever looking at their content
		string file1 = CreateBinaryFile("asset1.dat", BinaryVersion1);
		string file2 = CreateBinaryFile("asset2.dat", BinaryVersion2);

		bool mergeCallbackCalled = false;

		MergeResult? MergeCallback(string first, string second, string? existing)
		{
			mergeCallbackCalled = true;
			return FileDiffer.MergeFiles(first, second, MockFileSystem);
		}

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.dat",
			TestDirectory,
			MergeCallback,
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsFalse(mergeCallbackCalled, "Binary files should never be handed to the merge callback");
		Assert.IsTrue(result.Success, "Skipping binary files is a successful outcome, not a failure");
		CollectionAssert.AreEquivalent(new[] { file1, file2 }, result.SkippedBinaryFiles, "Both binary files should be reported as skipped");
		CollectionAssert.AreEqual(BinaryVersion1, MockFileSystem.File.ReadAllBytes(file1), "The first file's bytes should be untouched");
		CollectionAssert.AreEqual(BinaryVersion2, MockFileSystem.File.ReadAllBytes(file2), "The second file's bytes should be untouched");
	}

	[TestMethod]
	public void ProcessSinglePattern_WithTextAndBinaryFiles_MergesTextAndSkipsBinary()
	{
		// Arrange - one pattern, both kinds of file: the text versions still merge as before
		string text1 = CreateFile("notes1.dat", "line one\nline two\n");
		string text2 = CreateFile("notes2.dat", "line one\nline three\n");
		string binary = CreateBinaryFile("asset.dat", BinaryVersion1);

		List<string> mergedPairs = [];

		MergeResult? MergeCallback(string first, string second, string? existing)
		{
			mergedPairs.Add($"{first}|{second}");
			return FileDiffer.MergeFiles(first, second, MockFileSystem);
		}

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.dat",
			TestDirectory,
			MergeCallback,
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "The text files should still merge");
		Assert.AreEqual(1, mergedPairs.Count, "Only the two text files should have been merged");
		Assert.AreEqual($"{text1}|{text2}", mergedPairs[0], "The merge should have paired the two text files");
		CollectionAssert.AreEqual(new[] { binary }, result.SkippedBinaryFiles, "The binary file should be reported as skipped");
		CollectionAssert.AreEqual(BinaryVersion1, MockFileSystem.File.ReadAllBytes(binary), "The binary file's bytes should be untouched");
	}
}
