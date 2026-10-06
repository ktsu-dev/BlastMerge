// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="AsyncApplicationService"/>, driven against real files in a temporary directory.
/// </summary>
[TestClass]
[DoNotParallelize]
public class AsyncApplicationServiceTests : ConsoleTestBase
{
	/// <summary>
	/// Null arguments are rejected by every entry point.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task EntryPoints_NullArguments_Throw()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ProcessFilesAsync(null!, "*.txt")).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ProcessFilesAsync(TempDirectory, null!)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.CompareDirectoriesAsync(null!, TempDirectory, "*", false)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.CompareDirectoriesAsync(TempDirectory, null!, "*", false)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.CompareDirectoriesAsync(TempDirectory, TempDirectory, null!, false)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ComputeFileSimilarityAsync(null!, "b")).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ComputeFileSimilarityAsync("a", null!)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ReadFilesAsync(null!)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.CopyFilesAsync(null!)).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ProcessBatchAsync(null!, "batch")).ConfigureAwait(false);
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => AsyncApplicationService.ProcessBatchAsync(TempDirectory, null!)).ConfigureAwait(false);
	}

	/// <summary>
	/// Matching files are found recursively and grouped by content, ignoring files that do not match.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ProcessFilesAsync_GroupsMatchingFilesByContent()
	{
		string root = WriteFile("config.txt", "shared");
		string nested = WriteFile(Path.Join("sub", "config.txt"), "shared");
		string different = WriteFile(Path.Join("other", "config.txt"), "different");
		WriteFile("ignored.md", "shared");

		IReadOnlyDictionary<string, IReadOnlyCollection<string>> groups =
			await AsyncApplicationService.ProcessFilesAsync(TempDirectory, "config.txt", maxDegreeOfParallelism: 2).ConfigureAwait(false);

		Assert.HasCount(2, groups);
		IReadOnlyCollection<string> shared = groups.Values.Single(g => g.Count == 2);
		Assert.AreSequenceEqual(new[] { nested, root }.Order(StringComparer.Ordinal), shared.Order(StringComparer.Ordinal));
		Assert.AreSequenceEqual([different], groups.Values.Single(g => g.Count == 1));
		Assert.AreEqual(FileHasher.ComputeFileHash(root), groups.Single(g => g.Value.Count == 2).Key);
	}

	/// <summary>
	/// A pattern that matches nothing yields no groups.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ProcessFilesAsync_NoMatches_ReturnsEmpty()
	{
		WriteFile("a.txt", "a");

		IReadOnlyDictionary<string, IReadOnlyCollection<string>> groups =
			await AsyncApplicationService.ProcessFilesAsync(TempDirectory, "*.none").ConfigureAwait(false);

		Assert.IsEmpty(groups);
	}

	/// <summary>
	/// A non-recursive comparison classifies the top-level files and ignores subdirectories.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task CompareDirectoriesAsync_NonRecursive_ClassifiesTopLevelFiles()
	{
		(string dir1, string dir2) = CreateComparisonTree();

		DirectoryComparisonResult? result =
			await AsyncApplicationService.CompareDirectoriesAsync(dir1, dir2, "*.txt", recursive: false).ConfigureAwait(false);

		Assert.IsNotNull(result);
		Assert.AreSequenceEqual(["same.txt"], result.SameFiles);
		Assert.AreSequenceEqual(["modified.txt"], result.ModifiedFiles);
		Assert.AreSequenceEqual(["left.txt"], result.OnlyInDir1);
		Assert.AreSequenceEqual(["right.txt"], result.OnlyInDir2);
	}

	/// <summary>
	/// A recursive comparison also classifies files in subdirectories, by their relative path.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task CompareDirectoriesAsync_Recursive_IncludesSubdirectories()
	{
		(string dir1, string dir2) = CreateComparisonTree();

		DirectoryComparisonResult? result =
			await AsyncApplicationService.CompareDirectoriesAsync(dir1, dir2, "*.txt", recursive: true, maxDegreeOfParallelism: 1).ConfigureAwait(false);

		Assert.IsNotNull(result);
		Assert.AreSequenceEqual(
			new[] { "same.txt", Path.Join("nested", "deep.txt") }.Order(StringComparer.Ordinal),
			result.SameFiles.Order(StringComparer.Ordinal));
		Assert.AreSequenceEqual(
			new[] { "modified.txt", Path.Join("nested", "deep-modified.txt") }.Order(StringComparer.Ordinal),
			result.ModifiedFiles.Order(StringComparer.Ordinal));
		Assert.AreSequenceEqual(["left.txt"], result.OnlyInDir1);
		Assert.AreSequenceEqual(["right.txt"], result.OnlyInDir2);
	}

	/// <summary>
	/// A directory that does not exist contributes no files, whether or not the search is recursive.
	/// </summary>
	/// <param name="recursive">Whether the comparison is recursive.</param>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task CompareDirectoriesAsync_MissingDirectory_ReportsEverythingAsOneSided(bool recursive)
	{
		string existing = Path.Join(TempDirectory, "existing");
		WriteFile(Path.Join("existing", "only.txt"), "content");
		string missing = Path.Join(TempDirectory, "missing");

		DirectoryComparisonResult? forward =
			await AsyncApplicationService.CompareDirectoriesAsync(existing, missing, "*.txt", recursive).ConfigureAwait(false);
		DirectoryComparisonResult? backward =
			await AsyncApplicationService.CompareDirectoriesAsync(missing, existing, "*.txt", recursive).ConfigureAwait(false);

		Assert.IsNotNull(forward);
		Assert.AreSequenceEqual(["only.txt"], forward.OnlyInDir1);
		Assert.IsEmpty(forward.OnlyInDir2);
		Assert.IsEmpty(forward.SameFiles);
		Assert.IsEmpty(forward.ModifiedFiles);

		Assert.IsNotNull(backward);
		Assert.IsEmpty(backward.OnlyInDir1);
		Assert.AreSequenceEqual(["only.txt"], backward.OnlyInDir2);
	}

	/// <summary>
	/// Similarity is one for identical files and strictly between zero and one for partly shared ones.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ComputeFileSimilarityAsync_ScoresSharedLines()
	{
		string nl = Environment.NewLine;
		string first = WriteFile("first.txt", $"one{nl}two{nl}three{nl}four{nl}");
		string copy = WriteFile("copy.txt", $"one{nl}two{nl}three{nl}four{nl}");
		string partial = WriteFile("partial.txt", $"one{nl}two{nl}other{nl}else{nl}");
		string unrelated = WriteFile("unrelated.txt", $"alpha{nl}beta{nl}");

		double identical = await AsyncApplicationService.ComputeFileSimilarityAsync(first, copy).ConfigureAwait(false);
		double shared = await AsyncApplicationService.ComputeFileSimilarityAsync(first, partial).ConfigureAwait(false);
		double none = await AsyncApplicationService.ComputeFileSimilarityAsync(first, unrelated).ConfigureAwait(false);

		Assert.AreEqual(1.0, identical);
		Assert.IsGreaterThan(0.0, shared);
		Assert.IsLessThan(1.0, shared);
		Assert.AreEqual(0.0, none);
	}

	/// <summary>
	/// Every requested file is read, keyed by its path.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ReadFilesAsync_ReturnsEveryFilesContent()
	{
		string first = WriteFile("first.txt", "first content");
		string second = WriteFile(Path.Join("sub", "second.txt"), "second content");

		Dictionary<string, string> contents =
			await AsyncApplicationService.ReadFilesAsync([first, second], maxDegreeOfParallelism: 1).ConfigureAwait(false);

		Assert.HasCount(2, contents);
		Assert.AreEqual("first content", contents[first]);
		Assert.AreEqual("second content", contents[second]);
	}

	/// <summary>
	/// Copies are made into new directories as needed, and a copy whose source is missing is left out
	/// of the result rather than failing the rest.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task CopyFilesAsync_CopiesFilesAndOmitsFailures()
	{
		string source = WriteFile("source.txt", "payload");
		string target = Path.Join(TempDirectory, "new", "dir", "target.txt");
		string missingSource = Path.Join(TempDirectory, "missing.txt");
		string unreachedTarget = Path.Join(TempDirectory, "unreached.txt");

		IReadOnlyCollection<(string source, string target)> copied =
			await AsyncApplicationService.CopyFilesAsync([(source, target), (missingSource, unreachedTarget)]).ConfigureAwait(false);

		Assert.AreSequenceEqual([(source, target)], copied);
		Assert.AreEqual("payload", File.ReadAllText(target));
		Assert.IsFalse(File.Exists(unreachedTarget));
	}

	/// <summary>
	/// A batch that does not exist is reported and processes nothing.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ProcessBatchAsync_UnknownBatch_ReportsAndReturnsZero()
	{
		(int patternsProcessed, int totalFilesFound) =
			await AsyncApplicationService.ProcessBatchAsync(TempDirectory, "no-such-batch").ConfigureAwait(false);

		Assert.AreEqual(0, patternsProcessed);
		Assert.AreEqual(0, totalFilesFound);
		StringAssert.Contains(Output, "Batch configuration 'no-such-batch' not found.");
	}

	/// <summary>
	/// Patterns that match nothing are not counted when the batch skips empty patterns, and are counted
	/// when it does not; matching files are counted either way.
	/// </summary>
	/// <param name="skipEmptyPatterns">Whether the batch skips empty patterns.</param>
	/// <param name="expectedPatterns">The number of patterns expected to be counted.</param>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	[DataRow(true, 2)]
	[DataRow(false, 3)]
	public async Task ProcessBatchAsync_CountsPatternsAndFiles(bool skipEmptyPatterns, int expectedPatterns)
	{
		WriteFile("a.txt", "a");
		WriteFile(Path.Join("sub", "a.txt"), "a");
		WriteFile(Path.Join("sub", "b.txt"), "b");
		WriteFile("readme.md", "readme");
		SaveBatch(skipEmptyPatterns, promptBeforeEachPattern: false, "*.txt", "*.md", "*.none");

		(int patternsProcessed, int totalFilesFound) =
			await AsyncApplicationService.ProcessBatchAsync(TempDirectory, "test-batch").ConfigureAwait(false);

		Assert.AreEqual(expectedPatterns, patternsProcessed);
		Assert.AreEqual(4, totalFilesFound);
	}

	/// <summary>
	/// A batch that pauses before each pattern still processes every pattern.
	/// </summary>
	/// <returns>A task representing the asynchronous test.</returns>
	[TestMethod]
	public async Task ProcessBatchAsync_PromptBeforeEachPattern_StillProcessesEveryPattern()
	{
		WriteFile("a.txt", "a");
		SaveBatch(skipEmptyPatterns: true, promptBeforeEachPattern: true, "*.txt", "*.none");

		(int patternsProcessed, int totalFilesFound) =
			await AsyncApplicationService.ProcessBatchAsync(TempDirectory, "test-batch").ConfigureAwait(false);

		Assert.AreEqual(1, patternsProcessed);
		Assert.AreEqual(1, totalFilesFound);
	}

	private static void SaveBatch(bool skipEmptyPatterns, bool promptBeforeEachPattern, params string[] patterns)
	{
		BatchConfiguration batch = new()
		{
			Name = "test-batch",
			SkipEmptyPatterns = skipEmptyPatterns,
			PromptBeforeEachPattern = promptBeforeEachPattern,
		};

		foreach (string pattern in patterns)
		{
			batch.FilePatterns.Add(pattern);
		}

		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));
	}

	private (string dir1, string dir2) CreateComparisonTree()
	{
		WriteFile(Path.Join("left", "same.txt"), "same");
		WriteFile(Path.Join("right", "same.txt"), "same");
		WriteFile(Path.Join("left", "modified.txt"), "before");
		WriteFile(Path.Join("right", "modified.txt"), "after");
		WriteFile(Path.Join("left", "left.txt"), "left");
		WriteFile(Path.Join("right", "right.txt"), "right");
		WriteFile(Path.Join("left", "ignored.md"), "left");
		WriteFile(Path.Join("left", "nested", "deep.txt"), "deep");
		WriteFile(Path.Join("right", "nested", "deep.txt"), "deep");
		WriteFile(Path.Join("left", "nested", "deep-modified.txt"), "one");
		WriteFile(Path.Join("right", "nested", "deep-modified.txt"), "two");
		return (Path.Join(TempDirectory, "left"), Path.Join(TempDirectory, "right"));
	}
}
