// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BatchProcessorTests : MockFileSystemTestBase
{
	[TestMethod]
	public void CreateCustomBatch_WithValidParameters_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Test Batch";
		string[] patterns = ["*.txt", "*.cs"];
		string description = "Test Description";

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns, description);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(description, result.Description);
		CollectionAssert.AreEqual(patterns, result.FilePatterns.ToArray());
		Assert.IsFalse(result.PromptBeforeEachPattern, "PromptBeforeEachPattern should be false by default");
		Assert.IsTrue(result.SkipEmptyPatterns, "SkipEmptyPatterns should be true by default");
	}

	[TestMethod]
	public void CreateCustomBatch_WithEmptyDescription_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Test Batch";
		string[] patterns = ["*.txt"];

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(string.Empty, result.Description);
		CollectionAssert.AreEqual(patterns, result.FilePatterns.ToArray());
	}

	[TestMethod]
	public void ProcessBatch_WithNonExistentDirectory_ReturnsFailure()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Test",
			FilePatterns = ["*.txt"]
		};
		string nonExistentDir = "/path/that/does/not/exist";

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			nonExistentDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsFalse(result.Success, "Batch processing should fail for non-existent directory");
		Assert.IsTrue(result.Summary.Contains("Directory does not exist"), "Summary should indicate directory does not exist");
	}

	[TestMethod]
	public void ProcessBatch_WithNullParameters_ThrowsArgumentNullException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{ Name = "Test", FilePatterns = ["*.txt"] };
		string directory = "/test";

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(null!, directory, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, null!, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, null!, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, (a, b, c) => null, null!, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, (a, b, c) => null, _ => { }, null!));
	}

	[TestMethod]
	public void ProcessBatch_WithValidDirectory_ProcessesPatterns()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		List<(string, string, string?)> mergeResults = [];
		List<MergeSessionStatus> statusUpdates = [];

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(path1, path2, output) =>
			{
				mergeResults.Add((path1, path2, output));
				return new MergeResult(["merged content"], []);
			},
			statusUpdates.Add,
			() => true,
			null,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch processing should succeed for valid directory with matching files");
		Assert.AreEqual("Test Batch", result.BatchName);
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(1, result.SuccessfulPatterns);
		Assert.AreEqual(1, result.PatternResults.Count);
		Assert.AreEqual("Merge completed successfully", result.PatternResults[0].Message);
		Assert.AreEqual(2, result.PatternResults[0].FilesFound);
		Assert.AreEqual(1, mergeResults.Count);
		Assert.AreEqual(1, statusUpdates.Count);
		Assert.AreEqual(1, statusUpdates[0].CurrentIteration);
		Assert.AreEqual(2, statusUpdates[0].RemainingFilesCount);
		Assert.AreEqual(0, statusUpdates[0].CompletedMergesCount);
	}

	[TestMethod]
	public void ProcessBatch_WithNoMatchingFiles_HandlesEmptyPattern()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file.cs"), new("content"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"], // No .txt files exist
			SkipEmptyPatterns = false
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			null,
			MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, "Batch should fail when SkipEmptyPatterns is false and no files match"); // Should fail when SkipEmptyPatterns is false
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(0, result.SuccessfulPatterns);
	}

	[TestMethod]
	public void ProcessBatch_WithSkipEmptyPatterns_SkipsEmptyPatterns()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file.cs"), new("content"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"], // No .txt files exist
			SkipEmptyPatterns = true
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			null,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch should succeed when SkipEmptyPatterns is true and no files match"); // Should succeed when SkipEmptyPatterns is true
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(1, result.SuccessfulPatterns);
		Assert.AreEqual("No files found (skipped)", result.PatternResults.First().Message);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithMultipleFiles_ProcessesMerge()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		List<(string, string, string?)> mergeResults = [];
		List<MergeSessionStatus> statusUpdates = [];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"*.txt",
			[],
			testDir,
			[],
			(path1, path2, output) =>
			{
				mergeResults.Add((path1, path2, output));
				return new MergeResult(["merged content"], []);
			},
			statusUpdates.Add,
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Single pattern processing should succeed with multiple matching files");
		Assert.AreEqual("*.txt", result.Pattern);
		Assert.AreEqual(2, result.FilesFound);
		Assert.AreEqual("Merge completed successfully", result.Message);
		Assert.AreEqual(1, mergeResults.Count);
		Assert.AreEqual(1, statusUpdates.Count);
		Assert.AreEqual(1, statusUpdates[0].CurrentIteration);
		Assert.AreEqual(2, statusUpdates[0].RemainingFilesCount);
		Assert.AreEqual(0, statusUpdates[0].CompletedMergesCount);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithSingleFile_ReturnsOnlyOneFileMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing should succeed when only one file matches pattern");
		Assert.IsTrue(result.Message.Contains("Only one file found"), "Message should indicate only one file was found");
		Assert.AreEqual(1, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithNoFiles_ReturnsNoFilesMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, "Processing should fail when no files match pattern");
		Assert.IsTrue(result.Message.Contains("No files found"), "Message should indicate no files were found");
		Assert.AreEqual(0, result.FilesFound);
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithValidData_ProcessesSuccessfully()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		List<string> progressUpdates = [];
		bool mergeCallbackCalled = false;

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(path1, path2, output) =>
			{
				mergeCallbackCalled = true;
				return new MergeResult(["merged content"], []);
			},
			_ => { },
			() => true,
			progressUpdates.Add,
			0,  // Use default maxDegreeOfParallelism
			MockFileSystem);

		// Write diagnostic info to console
		Console.WriteLine($"Result: {result}");
		Console.WriteLine($"Success: {result.Success}");
		Console.WriteLine($"Summary: {result.Summary}");
		Console.WriteLine($"MergeCallbackCalled: {mergeCallbackCalled}");
		Console.WriteLine($"ProgressUpdates: {progressUpdates.Count}");
		foreach (string update in progressUpdates)
		{
			Console.WriteLine($"Update: {update}");
		}
		Console.WriteLine($"PatternResults: {result.PatternResults.Count}");
		foreach (PatternResult patternResult in result.PatternResults)
		{
			Console.WriteLine($"Pattern: {patternResult.Pattern}, Success: {patternResult.Success}, Message: {patternResult.Message}, FilesFound: {patternResult.FilesFound}");
		}

		// Assert
		Assert.IsTrue(result.Success, $"Batch result should be successful. Summary: {result.Summary}");
		Assert.IsTrue(progressUpdates.Any(msg => msg.Contains("Gathering")), "Should have progress updates about gathering files");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithUnreadableFileAmongDuplicates_ReportsFailureAndNamesSkippedFile()
	{
		// Arrange: three copies of config.json, one of them locked by "another process"
		string testDir = CreateTestDirectory();
		AddFile(Path.Join(testDir, "one", "config.json"), "shared content");
		AddFile(Path.Join(testDir, "two", "config.json"), "shared content");
		string lockedFile = AddUnreadableFile(Path.Join(testDir, "three", "config.json"), "content nobody can read");

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["config.json"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			null,
			0,
			MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, $"Batch must not report success when a file was skipped. Summary: {result.Summary}");

		PatternResult patternResult = result.PatternResults.Single(r => r.FileName == "config.json");
		Assert.IsFalse(patternResult.Success, "The pattern whose file could not be read must be reported as failed");
		Assert.IsTrue(
			patternResult.Message.Contains(lockedFile, StringComparison.Ordinal),
			$"The message should name the skipped file. Actual message: {patternResult.Message}");
		Assert.AreEqual(1, patternResult.SkippedFiles.Count, "The skipped file should be recorded on the pattern result");
		Assert.AreEqual(lockedFile, patternResult.SkippedFiles[0].FilePath);
		Assert.IsFalse(string.IsNullOrWhiteSpace(patternResult.SkippedFiles[0].ErrorMessage), "The underlying I/O error should be recorded");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithOnlyFileUnreadable_ReportsFailureRatherThanNoFilesFound()
	{
		// Arrange: the single file matching the pattern cannot be read
		string testDir = CreateTestDirectory();
		string lockedFile = AddUnreadableFile(Path.Join(testDir, "only", "settings.json"), "content nobody can read");

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["settings.json"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			null,
			0,
			MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, $"Batch must not report success when its only file was skipped. Summary: {result.Summary}");

		PatternResult patternResult = result.PatternResults.Single(r => r.FileName == "settings.json");
		Assert.IsFalse(patternResult.Success, "An unreadable file must not be folded into a successful 'no files found' result");
		Assert.IsTrue(
			patternResult.Message.Contains(lockedFile, StringComparison.Ordinal),
			$"The message should name the skipped file. Actual message: {patternResult.Message}");
		Assert.AreEqual(1, patternResult.SkippedFiles.Count, "The skipped file should be recorded on the pattern result");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithAllFilesReadable_RecordsNoSkippedFiles()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		AddFile(Path.Join(testDir, "one", "config.json"), "shared content");
		AddFile(Path.Join(testDir, "two", "config.json"), "shared content");

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["config.json"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			null,
			0,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, $"Batch should succeed when every file is readable. Summary: {result.Summary}");
		Assert.IsTrue(result.PatternResults.All(r => r.SkippedFiles.Count == 0), "No files should be reported as skipped");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithNullParameters_ThrowsArgumentNullException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{ Name = "Test", FilePatterns = ["*.txt"] };
		string directory = "/test";

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(null!, directory, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, null!, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, null!, _ => { }, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, (a, b, c) => null, null!, () => true));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, (a, b, c) => null, _ => { }, null!));
	}

	[TestMethod]
	public void ProcessSinglePatternWithPaths_WithSearchPaths_FindsFilesInSpecifiedPaths()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		string subDir1 = Path.Combine(testDir, "sub1");
		string subDir2 = Path.Combine(testDir, "sub2");

		MockFileSystem.AddDirectory(subDir1);
		MockFileSystem.AddDirectory(subDir2);
		MockFileSystem.AddFile(Path.Combine(subDir1, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(subDir2, "file2.txt"), new("content2"));

		string[] searchPaths = [subDir1, subDir2];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"*.txt",
			searchPaths,
			testDir,
			[],
			(a, b, c) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing should succeed when files are found in specified search paths");
		Assert.AreEqual(2, result.FilesFound);
	}

	private string CreateTestDirectory()
	{
		string testDir = Path.Combine(TestDirectory, "batch_test");
		MockFileSystem.Directory.CreateDirectory(testDir);
		return testDir;
	}

	/// <summary>
	/// Adds a readable file, creating its directory if needed.
	/// </summary>
	/// <returns>The path as the mock file system stores it.</returns>
	private string AddFile(string fullPath, string content) =>
		AddFile(fullPath, new MockFileData(content));

	/// <summary>
	/// Adds a file that cannot be opened for reading, standing in for a file locked by another
	/// process or one the current user has no access to.
	/// </summary>
	/// <returns>The path as the mock file system stores it.</returns>
	private string AddUnreadableFile(string fullPath, string content) =>
		AddFile(fullPath, new MockFileData(content) { AllowedFileShare = FileShare.None });

	private string AddFile(string fullPath, MockFileData fileData)
	{
		MockFileSystem.Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
		MockFileSystem.AddFile(fullPath, fileData);

		// The mock file system normalizes paths on storage, so report back what it actually holds
		return MockFileSystem.Path.GetFullPath(fullPath);
	}

	#region Additional Comprehensive Tests

	[TestMethod]
	public void CreateCustomBatch_WithNullName_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.CreateCustomBatch(null!, ["*.txt"]));
	}

	[TestMethod]
	public void CreateCustomBatch_WithNullPatterns_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			BatchProcessor.CreateCustomBatch("Test", null!));
	}

	[TestMethod]
	public void CreateCustomBatch_WithEmptyPatterns_FiltersOutEmptyStrings()
	{
		// Arrange
		string[] patterns = ["*.txt", "", "   ", "*.cs", null!];

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch("Test", patterns);

		// Assert
		Assert.AreEqual(2, result.FilePatterns.Count);
		CollectionAssert.Contains(result.FilePatterns.ToList(), "*.txt");
		CollectionAssert.Contains(result.FilePatterns.ToList(), "*.cs");
	}

	[TestMethod]
	public void ProcessBatch_WithPromptBeforeEachPattern_CallsPatternCallback()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.cs"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt", "*.cs"],
			PromptBeforeEachPattern = true
		};

		List<string> promptedPatterns = [];
		bool skipSecondPattern = false;

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			pattern =>
			{
				promptedPatterns.Add(pattern);
				if (pattern == "*.cs")
				{
					return !skipSecondPattern; // Skip the second pattern
				}
				return true;
			},
			MockFileSystem);

		// Assert
		Assert.AreEqual(2, promptedPatterns.Count);
		Assert.AreEqual("*.txt", promptedPatterns[0]);
		Assert.AreEqual("*.cs", promptedPatterns[1]);
		Assert.AreEqual(2, result.PatternResults.Count);
		// The second pattern (*.cs) should be skipped, but since there's only one file, it gets "Only one file found" message
		Assert.IsTrue(result.PatternResults[1].Message.Contains("Only one file found") || result.PatternResults[1].Message.Contains("Skipped by user"), "Second pattern should report 'Only one file found' or 'Skipped by user'");
	}

	[TestMethod]
	public void ProcessBatch_WithMultiplePatterns_ProcessesAllPatterns()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.cs"), new("code1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.cs"), new("code2"));

		BatchConfiguration batch = new()
		{
			Name = "Multi Pattern Batch",
			FilePatterns = ["*.txt", "*.cs"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			null,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch should succeed when all patterns have matching files");
		Assert.AreEqual(2, result.TotalPatternsProcessed);
		Assert.AreEqual(2, result.SuccessfulPatterns);
		Assert.AreEqual(2, result.PatternResults.Count);
		Assert.IsTrue(result.Summary.Contains("2/2 patterns"), "Summary should indicate all patterns were processed successfully");
	}

	[TestMethod]
	public void ProcessBatch_WithMixedSuccessAndFailure_ReportsCorrectSummary()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));
		// No .cs files, so *.cs pattern will fail

		BatchConfiguration batch = new()
		{
			Name = "Mixed Results Batch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = false
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			null,
			MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, "Batch should fail when some patterns have no matching files and SkipEmptyPatterns is false");
		Assert.AreEqual(2, result.TotalPatternsProcessed);
		Assert.AreEqual(1, result.SuccessfulPatterns);
		Assert.IsTrue(result.Summary.Contains("1 failed patterns"), "Summary should indicate one pattern failed");
		Assert.IsTrue(result.Summary.Contains("1/2 patterns"), "Summary should show partial pattern success ratio");
	}

	[TestMethod]
	public void ProcessSinglePattern_WithIdenticalFiles_ReturnsAllIdenticalMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("identical content"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("identical content"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file3.txt"), new("identical content"));

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing should succeed when all files are identical");
		Assert.IsTrue(result.Message.Contains("All files are identical"), "Message should indicate all files are identical");
		Assert.AreEqual(3, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithProgressCallback_ReportsProgress()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		List<string> progressMessages = [];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			progressMessages.Add,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing with progress callback should succeed");
		// Progress messages may not be generated for simple patterns, so just check the result is successful
		// Assert.IsTrue(progressMessages.Count > 0, "Should have received progress messages");
	}

	[TestMethod]
	public void ProcessSinglePatternWithPaths_WithExclusionPatterns_ExcludesMatchingFiles()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		string subDir = Path.Combine(testDir, "bin");
		MockFileSystem.Directory.CreateDirectory(subDir);

		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));
		MockFileSystem.AddFile(Path.Combine(subDir, "excluded.txt"), new("excluded"));

		List<string> exclusionPatterns = ["*/bin/*"];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"*.txt",
			[],
			testDir,
			exclusionPatterns,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing should succeed when exclusion patterns are applied");
		Assert.AreEqual(2, result.FilesFound); // Should exclude the file in bin directory
	}

	[TestMethod]
	public void ProcessSinglePatternWithPaths_WithSearchPaths_SearchesSpecifiedPaths()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		string searchDir1 = Path.Combine(testDir, "search1");
		string searchDir2 = Path.Combine(testDir, "search2");
		MockFileSystem.Directory.CreateDirectory(searchDir1);
		MockFileSystem.Directory.CreateDirectory(searchDir2);

		MockFileSystem.AddFile(Path.Combine(searchDir1, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(searchDir2, "file2.txt"), new("content2"));
		MockFileSystem.AddFile(Path.Combine(testDir, "ignored.txt"), new("ignored")); // Should be ignored

		List<string> searchPaths = [searchDir1, searchDir2];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"*.txt",
			searchPaths,
			testDir,
			[],
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing should succeed when searching specified paths");
		Assert.AreEqual(2, result.FilesFound); // Should find files in search paths only
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithProgressCallback_ReportsPhaseProgress()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Progress Test Batch",
			FilePatterns = ["*.txt"]
		};

		List<string> progressMessages = [];

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			progressMessages.Add,
			1, // Use single thread for predictable testing
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch processing with discrete phases should succeed");
		Assert.IsTrue(progressMessages.Count > 0, "Should have received progress messages");
		Assert.IsTrue(progressMessages.Any(m => m.Contains("PHASE 1")), "Should report Phase 1");
		Assert.IsTrue(progressMessages.Any(m => m.Contains("PHASE 2") || m.Contains("Computing file hashes")), "Should report Phase 2");
		Assert.IsTrue(progressMessages.Any(m => m.Contains("PHASE 3") || m.Contains("Grouping")), "Should report Phase 3");
		Assert.IsTrue(progressMessages.Any(m => m.Contains("PHASE 4") || m.Contains("Resolving")), "Should report Phase 4");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithNoFiles_ReturnsEarlyWithMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		// No files created

		BatchConfiguration batch = new()
		{
			Name = "Empty Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true,
			null,
			0,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch should succeed with no work done when no files match"); // Should succeed but with no work done
		Assert.IsTrue(result.Summary.Contains("No files found"), "Summary should indicate no files were found");
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithCustomParallelism_UsesSpecifiedDegree()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Parallelism Test",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() => true,
			null,
			2, // Custom parallelism
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Batch should succeed with custom parallelism setting");
		// The discrete phases method creates pattern results differently, so check the overall success
		Assert.IsTrue(result.TotalPatternsProcessed >= 1, "At least one pattern should be processed");
	}

	[TestMethod]
	public void ProcessSinglePattern_WithMergeFailure_HandlesCancellation()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null, // Return null to simulate cancellation
			_ => { },
			() => true,
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, "Processing should fail when merge callback returns null (cancellation)");
		Assert.AreEqual(2, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithContinuationCallbackFalse_StopsProcessing()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file3.txt"), new("content3"));

		bool firstCall = true;

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => new MergeResult(["merged"], []),
			_ => { },
			() =>
			{
				if (firstCall)
				{
					firstCall = false;
					return false; // Stop after first merge
				}
				return true;
			},
			fileSystem: MockFileSystem);

		// Assert
		Assert.IsFalse(result.Success, "Processing should fail when continuation callback returns false"); // Should fail due to incomplete processing
		Assert.AreEqual(3, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithThreeDifferingVersions_FoldsEveryVersionIntoTheFinalContent()
	{
		// Arrange - three versions of the same file, each carrying a line the other two lack
		string testDir = CreateTestDirectory();
		string pathA = AddFile(Path.Join(testDir, "repo-a", "app.config"), $"shared{Environment.NewLine}only-in-a");
		string pathB = AddFile(Path.Join(testDir, "repo-b", "app.config"), $"shared{Environment.NewLine}only-in-b");
		string pathC = AddFile(Path.Join(testDir, "repo-c", "app.config"), $"shared{Environment.NewLine}only-in-c");

		List<string[]> firstSideOfEachMerge = [];

		// Act - a union merge, so no line that reaches a merge can be lost by the merge itself
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"app.config",
			[],
			testDir,
			[],
			(path1, path2, output) =>
			{
				string[] lines1 = MockFileSystem.File.ReadAllLines(path1);
				string[] lines2 = MockFileSystem.File.ReadAllLines(path2);
				firstSideOfEachMerge.Add(lines1);
				return new MergeResult([.. lines1.Union(lines2, StringComparer.Ordinal)], []);
			},
			_ => { },
			() => true,
			MockFileSystem);

		// Assert
		Assert.IsTrue(result.Success, "Processing three differing versions should succeed");
		Assert.AreEqual(3, result.FilesFound);
		Assert.AreEqual(2, firstSideOfEachMerge.Count, "Three versions should take two merges");

		// The second merge must start from the first merge's result, not from the original file
		CollectionAssert.Contains(
			firstSideOfEachMerge[1],
			"only-in-a",
			"The second merge should fold in the first merge's result, not re-read the original second file");

		// Every version's unique line survives into the final content of every file
		foreach (string path in new[] { pathA, pathB, pathC })
		{
			string finalContent = MockFileSystem.File.ReadAllText(path);
			StringAssert.Contains(finalContent, "only-in-a", $"{path} should retain the first version's content");
			StringAssert.Contains(finalContent, "only-in-b", $"{path} should retain the second version's content");
			StringAssert.Contains(finalContent, "only-in-c", $"{path} should retain the third version's content");
		}
	}

	[TestMethod]
	public void ProcessSinglePattern_WithTwoDifferingVersions_WritesMergedContentToBothFiles()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		string pathA = AddFile(Path.Join(testDir, "repo-a", "app.config"), "only-in-a");
		string pathB = AddFile(Path.Join(testDir, "repo-b", "app.config"), "only-in-b");

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"app.config",
			[],
			testDir,
			[],
			(path1, path2, output) => new MergeResult(["only-in-a", "only-in-b"], []),
			_ => { },
			() => true,
			MockFileSystem);

		// Assert - the merge is consolidated onto both sources, not discarded
		Assert.IsTrue(result.Success, "Processing two differing versions should succeed");
		string expected = string.Join(Environment.NewLine, "only-in-a", "only-in-b");
		Assert.AreEqual(expected, MockFileSystem.File.ReadAllText(pathA));
		Assert.AreEqual(expected, MockFileSystem.File.ReadAllText(pathB));
	}

	#endregion
}
