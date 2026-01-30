// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EdgeCaseTests : MockFileSystemTestBase
{
	private FileDifferAdapter _fileDifferAdapter = null!;
	private FileHasherAdapter _fileHasherAdapter = null!;
	private FileFinderAdapter _fileFinderAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Initialize adapters
		_fileDifferAdapter = new FileDifferAdapter(MockFileSystem);
		_fileHasherAdapter = new FileHasherAdapter(MockFileSystem);
		_fileFinderAdapter = new FileFinderAdapter(MockFileSystem);
	}

	[TestMethod]
	public void FileDiffer_EmptyFiles_ProducesCorrectDiff()
	{
		// Arrange
		string emptyFile1 = CreateFile("empty1.txt", string.Empty);
		string emptyFile2 = CreateFile("empty2.txt", string.Empty);

		// Act
		IReadOnlyCollection<LineDifference> differences = _fileDifferAdapter.FindDifferences(emptyFile1, emptyFile2);
		string gitDiff = _fileDifferAdapter.GenerateGitStyleDiff(emptyFile1, emptyFile2);

		// Assert
		Assert.AreEqual(0, differences.Count, "Empty files should have no differences");
		Assert.AreEqual(string.Empty, gitDiff, "Git diff for empty files should be empty string");
	}

	[TestMethod]
	public void FileHasher_IdenticalContentDifferentEncoding_SameHash()
	{
		// Arrange - Create files with same content but different encoding
		string utf8Content = "Test content with some unicode: äöü";
		byte[] utf8Bytes = Encoding.UTF8.GetBytes(utf8Content);
		byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf8Content);

		string utf8File = CreateFile("utf8.txt", "");
		string utf16File = CreateFile("utf16.txt", "");

		// Write the bytes directly to simulate different encodings
		MockFileSystem.File.WriteAllBytes(utf8File, utf8Bytes);
		MockFileSystem.File.WriteAllBytes(utf16File, utf16Bytes);

		// Act
		string hash1 = _fileHasherAdapter.ComputeFileHash(utf8File);
		string hash2 = _fileHasherAdapter.ComputeFileHash(utf16File);

		// Assert
		// Note: We expect different hashes because the actual byte content is different
		// due to different encodings, even though the text appears the same
		Assert.AreNotEqual(hash1, hash2, "Files with same text but different encodings should have different hashes");
	}

	[TestMethod]
	public void FileFinder_SpecialCharactersInFilename_FindsCorrectly()
	{
		// Arrange - Create files with special characters in names
		string specialFileName = "test #$%^&.txt";
		string specialFilePath = CreateFile(specialFileName, "Test content");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, specialFileName);

		// Assert
		Assert.AreEqual(1, files.Count, "Should find the file with special characters in name");
		Assert.AreEqual(specialFilePath, files.First(), "Should return the correct file path");
	}

	[TestMethod]
	public void SyncFile_TargetDirectoryDoesNotExist_CreatesDirectory()
	{
		// Arrange
		string sourceFile = CreateFile("source.txt", "Test content");
		string targetDir = Path.Combine(TestDirectory, "NonExistentDir");
		string targetFile = Path.Combine(targetDir, "target.txt");

		// Ensure directory doesn't exist in mock file system
		Assert.IsFalse(MockFileSystem.Directory.Exists(targetDir), "Target directory should not exist before sync");

		// Act
		_fileDifferAdapter.SyncFile(sourceFile, targetFile);

		// Assert
		Assert.IsTrue(MockFileSystem.Directory.Exists(targetDir), "Target directory should be created");
		Assert.IsTrue(MockFileSystem.File.Exists(targetFile), "Target file should exist");
		Assert.AreEqual("Test content", MockFileSystem.File.ReadAllText(targetFile), "Content should be copied correctly");
	}

	[TestMethod]
	public void FileDiffer_GroupFilesByFilenameAndHash_PreventsUnrelatedFilesFromBeingGrouped()
	{
		// Arrange - Create files with different names but similar content patterns
		string configFile1 = CreateFile("app.config", "<configuration><setting>value1</setting></configuration>");
		string configFile2 = CreateFile("web.config", "<configuration><setting>value2</setting></configuration>");
		string configFile3 = CreateFile("database.config", "<configuration><setting>value3</setting></configuration>");

		// Also create duplicate files with same names but in different locations
		string subDir = CreateDirectory("subdir");
		string appConfigCopy = CreateFile("subdir/app.config", "<configuration><setting>value1</setting></configuration>");
		string appConfigModified = CreateFile("subdir2/app.config", "<configuration><setting>modified</setting></configuration>");

		List<string> allFiles = [configFile1, configFile2, configFile3, appConfigCopy, appConfigModified];

		// Act
		IReadOnlyCollection<FileGroup> groups = _fileDifferAdapter.GroupFilesByFilenameAndHash(allFiles);

		// Assert
		Assert.AreEqual(4, groups.Count, "Should have 4 groups: 1 for web.config, 1 for database.config, 2 for different app.config versions");

		// Verify that files with different names are in separate groups
		FileGroup[] groupArray = [.. groups];

		// Find groups containing each file type
		FileGroup? webConfigGroup = groupArray.FirstOrDefault(g => g.FilePaths.Any(p => Path.GetFileName(p) == "web.config"));
		FileGroup? databaseConfigGroup = groupArray.FirstOrDefault(g => g.FilePaths.Any(p => Path.GetFileName(p) == "database.config"));

		Assert.IsNotNull(webConfigGroup, "Should have a group for web.config");
		Assert.IsNotNull(databaseConfigGroup, "Should have a group for database.config");

		// Verify web.config and database.config are not in the same group
		Assert.AreEqual(1, webConfigGroup.FilePaths.Count, "web.config should be in its own group");
		Assert.AreEqual(1, databaseConfigGroup.FilePaths.Count, "database.config should be in its own group");

		// Verify app.config files are properly grouped by content
		List<FileGroup> appConfigGroups = [.. groupArray.Where(g => g.FilePaths.Any(p => Path.GetFileName(p) == "app.config"))];
		Assert.AreEqual(2, appConfigGroups.Count, "Should have 2 groups for app.config files (identical and modified)");

		// One group should have 2 identical files, other should have 1 modified file
		List<int> groupSizes = [.. appConfigGroups.Select(g => g.FilePaths.Count).OrderBy(size => size)];
		Assert.AreEqual(1, groupSizes[0], "One app.config group should have 1 file (modified version)");
		Assert.AreEqual(2, groupSizes[1], "One app.config group should have 2 files (identical versions)");
	}

	[TestMethod]
	public void FileDiffer_GroupFilesByHashOnly_AllowsUnrelatedFilesToBeGrouped()
	{
		// Arrange - Create files with different names but identical content
		string file1 = CreateFile("readme.txt", "This is identical content");
		string file2 = CreateFile("notes.txt", "This is identical content");
		string file3 = CreateFile("config.txt", "This is identical content");

		List<string> allFiles = [file1, file2, file3];

		// Act - Use the legacy hash-only grouping
		IReadOnlyCollection<FileGroup> groups = _fileDifferAdapter.GroupFilesByHashOnly(allFiles);

		// Assert - Should group all files together despite having different names
		Assert.AreEqual(1, groups.Count, "Hash-only grouping should put all identical content files in one group");

		FileGroup group = groups.First();
		Assert.AreEqual(3, group.FilePaths.Count, "All 3 files should be in the same group");

		// Verify all different filenames are present
		List<string?> filenames = [.. group.FilePaths.Select(Path.GetFileName).OrderBy(name => name)];
		Assert.AreEqual("config.txt", filenames[0]);
		Assert.AreEqual("notes.txt", filenames[1]);
		Assert.AreEqual("readme.txt", filenames[2]);
	}

	[TestMethod]
	public void FileDiffer_GroupFilesByFilenameAndHash_HandlesSingleFilesCorrectly()
	{
		// Arrange - Create files where each filename appears only once
		string file1 = CreateFile("unique1.txt", "Content 1");
		string file2 = CreateFile("unique2.txt", "Content 2");
		string file3 = CreateFile("unique3.txt", "Content 3");

		List<string> allFiles = [file1, file2, file3];

		// Act
		IReadOnlyCollection<FileGroup> groups = _fileDifferAdapter.GroupFilesByFilenameAndHash(allFiles);

		// Assert - Each file should be in its own group
		Assert.AreEqual(3, groups.Count, "Each unique filename should be in its own group");

		foreach (FileGroup group in groups)
		{
			Assert.AreEqual(1, group.FilePaths.Count, "Each group should contain exactly one file");
		}
	}

	[TestMethod]
	public void FindMostSimilarFiles_WithDifferentFilenames_ShouldNotCompare()
	{
		// Arrange: Create files with different names but similar content
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nline2\nline4"; // Only last line differs

		string file1 = CreateFile("update-readme.yml", content1);
		string file2 = CreateFile("dotnet-sdk.yml", content2);

		List<string> allFiles = [file1, file2];

		// Act: Group files and find most similar
		IReadOnlyCollection<FileGroup> fileGroups = _fileDifferAdapter.GroupFilesByFilenameAndHash(allFiles);
		FileSimilarity? similarity = _fileDifferAdapter.FindMostSimilarFiles(fileGroups);

		// Assert: Should not find any similar files (different filenames)
		Assert.IsNull(similarity, "FindMostSimilarFiles should return null when files have different names");
		Assert.AreEqual(2, fileGroups.Count, "Should have 2 separate groups for different filenames");
	}

	[TestMethod]
	public void FindMostSimilarFiles_WithSameFilenames_ShouldCompare()
	{
		// Arrange: Create files with same names and similar content
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nline2\nline4"; // Only last line differs

		string file1 = CreateFile("dir1/config.yml", content1);
		string file2 = CreateFile("dir2/config.yml", content2);

		List<string> allFiles = [file1, file2];

		// Act: Group files and find most similar
		IReadOnlyCollection<FileGroup> fileGroups = _fileDifferAdapter.GroupFilesByFilenameAndHash(allFiles);
		FileSimilarity? similarity = _fileDifferAdapter.FindMostSimilarFiles(fileGroups);

		// Assert: Should find similarity between files with same names
		Assert.IsNotNull(similarity, "FindMostSimilarFiles should find similar files with same names");
		Assert.IsTrue(similarity.SimilarityScore > 0, "Similarity score should be greater than 0");

		// Verify both files have the same filename
		string filename1 = Path.GetFileName(similarity.FilePath1);
		string filename2 = Path.GetFileName(similarity.FilePath2);
		Assert.AreEqual("config.yml", filename1);
		Assert.AreEqual("config.yml", filename2);
	}
}
