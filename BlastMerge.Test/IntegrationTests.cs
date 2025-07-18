// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Integration tests that verify end-to-end workflows combining multiple services
/// </summary>
[TestClass]
public class IntegrationTests : DependencyInjectionTestBase
{
	private string _testDirectory = null!;
	private DiffPlexDiffer _diffPlexDiffer = null!;
	private FileFinder _fileFinder = null!;

	protected override void InitializeTestData()
	{
		// Get services from DI
		_diffPlexDiffer = GetService<DiffPlexDiffer>();
		_fileFinder = GetService<FileFinder>();

		// Set up test directory (relative to the DI test base directory)
		_testDirectory = CreateDirectory("integration_test");

		// Create a realistic project structure for testing
		CreateFile("integration_test/README.md", "# Project\n\nThis is a sample project.");
		CreateFile("integration_test/LICENSE", "MIT License\n\nCopyright (c) 2024");
		CreateFile("integration_test/src/main.cs", "using System;\n\nclass Program {\n    static void Main() {\n        Console.WriteLine(\"Hello World\");\n    }\n}");
		CreateFile("integration_test/src/utils.cs", "using System;\n\nclass Utils {\n    public static void Log(string message) {\n        Console.WriteLine(message);\n    }\n}");
		CreateFile("integration_test/tests/main_test.cs", "using Microsoft.VisualStudio.TestTools.UnitTesting;\n\n[TestClass]\npublic class MainTests {\n    [TestMethod]\n    public void TestMain() {\n        Assert.IsTrue(true);\n    }\n}");

		// Create similar files in different directories for merging scenarios
		CreateFile("integration_test/repo1/config.json", /*lang=json,strict*/ "{\n  \"name\": \"project1\",\n  \"version\": \"1.0.0\",\n  \"debug\": true\n}");
		CreateFile("integration_test/repo2/config.json", /*lang=json,strict*/ "{\n  \"name\": \"project2\",\n  \"version\": \"1.0.0\",\n  \"debug\": false\n}");
		CreateFile("integration_test/repo3/config.json", /*lang=json,strict*/ "{\n  \"name\": \"project1\",\n  \"version\": \"1.0.0\",\n  \"debug\": true\n}"); // Identical to repo1

		// Create files for async differ testing
		CreateFile("integration_test/async_test/file_a.txt", "Line 1\nLine 2\nLine 3");
		CreateFile("integration_test/async_test/file_b.txt", "Line 1\nModified Line 2\nLine 3");
		CreateFile("integration_test/async_test/file_c.txt", "Line 1\nLine 2\nLine 3"); // Identical to file_a
	}

	/// <summary>
	/// Integration test: Find files, group by hash, and verify grouping accuracy
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_FindGroupAndHashFiles_WorksCorrectly()
	{
		// Arrange
		string searchPattern = "config.json";

		// Act - Find all config.json files
		IReadOnlyCollection<string> foundFiles = _fileFinder.FindFiles(_testDirectory, searchPattern);

		// Group by hash using AsyncFileDiffer
		IReadOnlyCollection<FileGroup> groups = await AsyncFileDiffer.GroupFilesByHashAsync(foundFiles).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(3, foundFiles.Count, "Should find 3 config.json files");
		Assert.AreEqual(2, groups.Count, "Should have 2 groups (identical files should be grouped together)");

		// Verify one group has 2 files (repo1 and repo3 are identical)
		FileGroup? largerGroup = groups.FirstOrDefault(g => g.FilePaths.Count > 1);
		Assert.IsNotNull(largerGroup, "Should have a group with multiple files");
		Assert.AreEqual(2, largerGroup.FilePaths.Count, "Should have 2 identical files");
		Assert.IsTrue(largerGroup.FilePaths.Any(p => p.Contains("repo1")), "Should include repo1 file");
		Assert.IsTrue(largerGroup.FilePaths.Any(p => p.Contains("repo3")), "Should include repo3 file");

		// Verify one group has 1 file (repo2 is different)
		FileGroup? singleGroup = groups.FirstOrDefault(g => g.FilePaths.Count == 1);
		Assert.IsNotNull(singleGroup, "Should have a group with single file");
		Assert.IsTrue(singleGroup.FilePaths.First().Contains("repo2"), "Should be the repo2 file");
	}

	/// <summary>
	/// Integration test: Calculate similarity between multiple files
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_SimilarityCalculation_WorksCorrectly()
	{
		// Arrange
		string fileA = Path.Combine(_testDirectory, "async_test", "file_a.txt");
		string fileB = Path.Combine(_testDirectory, "async_test", "file_b.txt");
		string fileC = Path.Combine(_testDirectory, "async_test", "file_c.txt");

		// Act
		double similarityAB = await AsyncFileDiffer.CalculateFileSimilarityAsync(fileA, fileB).ConfigureAwait(false);
		double similarityAC = await AsyncFileDiffer.CalculateFileSimilarityAsync(fileA, fileC).ConfigureAwait(false);
		double similarityBC = await AsyncFileDiffer.CalculateFileSimilarityAsync(fileB, fileC).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(1.0, similarityAC, 0.001, "Files A and C should be identical");
		Assert.IsTrue(similarityAB is > 0.0 and < 1.0, $"Files A and B should be similar but not identical, got {similarityAB}");
		Assert.IsTrue(similarityBC is > 0.0 and < 1.0, $"Files B and C should be similar but not identical, got {similarityBC}");
	}

	/// <summary>
	/// Integration test: Diff generation workflow
	/// </summary>
	[TestMethod]
	public void EndToEnd_DiffGeneration_WorksCorrectly()
	{
		// Arrange
		string file1 = Path.Combine(_testDirectory, "repo1", "config.json");
		string file2 = Path.Combine(_testDirectory, "repo2", "config.json");

		// Act
		IReadOnlyCollection<LineDifference> differences = _diffPlexDiffer.FindDifferences(file1, file2);
		string gitStyleDiff = _diffPlexDiffer.GenerateUnifiedDiff(file1, file2, 2);

		// Assert
		Assert.IsTrue(differences.Count > 0, "Should find differences between the files");
		Assert.IsTrue(gitStyleDiff.Contains("project1"), "Diff should contain content from first file");
		Assert.IsTrue(gitStyleDiff.Contains("project2"), "Diff should contain content from second file");
		Assert.IsTrue(gitStyleDiff.Contains("true"), "Diff should show debug:true");
		Assert.IsTrue(gitStyleDiff.Contains("false"), "Diff should show debug:false");
	}

	/// <summary>
	/// Integration test: File reading and content verification
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_FileReading_WorksCorrectly()
	{
		// Arrange
		List<string> filesToRead = [
			Path.Combine(_testDirectory, "README.md"),
			Path.Combine(_testDirectory, "LICENSE"),
			Path.Combine(_testDirectory, "src", "main.cs")
		];

		// Act
		Dictionary<string, string> fileContents = await AsyncFileDiffer.ReadFilesAsync(filesToRead).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(3, fileContents.Count, "Should read all 3 files");
		Assert.IsTrue(fileContents[filesToRead[0]].Contains("# Project"), "README should contain header");
		Assert.IsTrue(fileContents[filesToRead[1]].Contains("MIT License"), "LICENSE should contain license text");
		Assert.IsTrue(fileContents[filesToRead[2]].Contains("Hello World"), "Main.cs should contain hello world");
	}

	/// <summary>
	/// Integration test: File copying workflow
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_FileCopying_WorksCorrectly()
	{
		// Arrange
		string sourceFile = Path.Combine(_testDirectory, "README.md");
		string targetFile = Path.Combine(_testDirectory, "backup", "README_backup.md");
		List<(string source, string target)> copyOperations = [(sourceFile, targetFile)];

		// Act
		IReadOnlyCollection<(string source, string target)> results = await AsyncFileDiffer.CopyFilesAsync(copyOperations).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(1, results.Count, "Should successfully copy 1 file");
		Assert.AreEqual(sourceFile, results.First().source, "Should return correct source");
		Assert.AreEqual(targetFile, results.First().target, "Should return correct target");

		// Verify file was actually copied
		Assert.IsTrue(MockFileSystem.File.Exists(targetFile), "Target file should exist");
		string originalContent = MockFileSystem.File.ReadAllText(sourceFile);
		string copiedContent = MockFileSystem.File.ReadAllText(targetFile);
		Assert.AreEqual(originalContent, copiedContent, "Copied content should match original");
	}

	/// <summary>
	/// Integration test: Grouping by filename and hash
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_GroupByFilenameAndHash_WorksCorrectly()
	{
		// Arrange
		List<string> configFiles = [
			Path.Combine(_testDirectory, "repo1", "config.json"),
			Path.Combine(_testDirectory, "repo2", "config.json"),
			Path.Combine(_testDirectory, "repo3", "config.json")
		];

		// Act
		IReadOnlyCollection<FileGroup> groups = await AsyncFileDiffer.GroupFilesByFilenameAndHashAsync(configFiles).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(2, groups.Count, "Should create 2 groups (repo1&repo3 together, repo2 separate)");

		// Verify grouping
		bool hasGroupWithTwoFiles = groups.Any(g => g.FilePaths.Count == 2);
		bool hasGroupWithOneFile = groups.Any(g => g.FilePaths.Count == 1);

		Assert.IsTrue(hasGroupWithTwoFiles, "Should have a group with 2 files");
		Assert.IsTrue(hasGroupWithOneFile, "Should have a group with 1 file");
	}

	/// <summary>
	/// Integration test: Error handling across services
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_ErrorHandling_WorksCorrectly()
	{
		// Arrange - use non-existent files
		List<string> nonExistentFiles = [
			Path.Combine(_testDirectory, "does_not_exist.txt"),
			Path.Combine(_testDirectory, "also_missing.txt")
		];

		// Act & Assert - ReadFilesAsync should handle missing files gracefully
		await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
			await AsyncFileDiffer.ReadFilesAsync(nonExistentFiles).ConfigureAwait(false)).ConfigureAwait(false);

		// Act & Assert - CalculateFileSimilarityAsync should handle missing files
		await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
			await AsyncFileDiffer.CalculateFileSimilarityAsync(nonExistentFiles[0], nonExistentFiles[1]).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Integration test: Performance characteristics with multiple files
	/// </summary>
	[TestMethod]
	public async Task EndToEnd_PerformanceWithMultipleFiles_CompletesReasonably()
	{
		// Arrange - Create many small files
		for (int i = 0; i < 50; i++)
		{
			CreateFile($"integration_test/perf_test/file_{i:000}.txt", $"Content of file {i}\nSecond line\nThird line {i % 10}");
		}

		List<string> allFiles = [];
		for (int i = 0; i < 50; i++)
		{
			allFiles.Add(Path.Combine(_testDirectory, "perf_test", $"file_{i:000}.txt"));
		}

		// Act - Measure performance
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		IReadOnlyCollection<FileGroup> groups = await AsyncFileDiffer.GroupFilesByHashAsync(allFiles).ConfigureAwait(false);
		stopwatch.Stop();

		// Assert
		Assert.IsTrue(groups.Count > 0, "Should create groups");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, $"Should complete within 10 seconds, took {stopwatch.ElapsedMilliseconds}ms");

		// Verify logical correctness
		int totalFiles = groups.Sum(g => g.FilePaths.Count);
		Assert.AreEqual(50, totalFiles, "All files should be accounted for in groups");
	}
}
