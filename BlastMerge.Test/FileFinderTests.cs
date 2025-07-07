// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileFinderTests : DependencyInjectionTestBase
{
	private FileFinderAdapter _fileFinderAdapter = null!;

	protected override void InitializeTestData()
	{
		// Create test files with the same name in different directories
		CreateFile(MockFileSystem.Path.Combine("test.txt"), "Root test file");
		CreateFile(MockFileSystem.Path.Combine("Subdir1", "test.txt"), "Subdir1 test file");
		CreateFile(MockFileSystem.Path.Combine("Subdir2", "test.txt"), "Subdir2 test file");
		CreateFile(MockFileSystem.Path.Combine("Subdir1", "NestedSubdir", "test.txt"), "Nested test file");

		// Create some files with different names
		CreateFile(MockFileSystem.Path.Combine("other.txt"), "Other file");
		CreateFile(MockFileSystem.Path.Combine("Subdir1", "different.txt"), "Different file");

		// Create additional structure for search path and exclusion tests
		CreateFile(MockFileSystem.Path.Combine("bin", "test.txt"), "Bin test file");
		CreateFile(MockFileSystem.Path.Combine("obj", "test.txt"), "Obj test file");
		CreateFile(MockFileSystem.Path.Combine("temp", "test.txt"), "Temp test file");
		CreateFile(MockFileSystem.Path.Combine("node_modules", "test.txt"), "Node modules test file");
		CreateFile(MockFileSystem.Path.Combine("SearchPath1", "test.txt"), "Search path 1 test file");
		CreateFile(MockFileSystem.Path.Combine("SearchPath2", "test.txt"), "Search path 2 test file");

		// Initialize the adapter
		_fileFinderAdapter = new FileFinderAdapter(MockFileSystem);
	}

	[TestMethod]
	public void FindFiles_NonExistingFileName_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "nonexistent.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent files");
	}

	[TestMethod]
	public void FindFiles_NonExistentDirectory_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(MockFileSystem.Path.Combine(TestDirectory, "NonExistentDir"), "test.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent directory");
	}

	[TestMethod]
	public void FindFiles_WithWildcard_ReturnsAllMatches()
	{
		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "*.txt");

		// Assert
		Assert.AreEqual(12, files.Count, "Should find all 12 .txt files");
	}

	[TestMethod]
	public void FindFiles_SkipsGitSubmodules()
	{
		// Arrange - Create a submodule structure
		string submoduleDir = MockFileSystem.Path.Combine(TestDirectory, "MySubmodule");
		MockFileSystem.Directory.CreateDirectory(submoduleDir);

		// Create a .git file (not directory) which indicates this is a submodule
		MockFileSystem.File.WriteAllText(MockFileSystem.Path.Combine(submoduleDir, ".git"), "gitdir: ../.git/modules/MySubmodule");

		// Create a test file inside the submodule that should be skipped
		CreateFile(MockFileSystem.Path.Combine("MySubmodule", "test.txt"), "Submodule test file");

		// Create another file in a nested directory within the submodule
		CreateFile(MockFileSystem.Path.Combine("MySubmodule", "SubDir", "test.txt"), "Nested submodule test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(10, files.Count, "Should find 10 test.txt files, excluding those in submodules");
		Assert.IsFalse(files.Any(f => f.Contains("MySubmodule")), "Should not include files from submodule directories");
	}

	[TestMethod]
	public void FindFiles_IncludesRegularGitRepositories()
	{
		// Arrange - Create a regular git repository (not a submodule)
		string gitRepoDir = MockFileSystem.Path.Combine(TestDirectory, "MyGitRepo");
		MockFileSystem.Directory.CreateDirectory(gitRepoDir);

		// Create a .git directory (not file) which indicates this is a regular git repo, not a submodule
		MockFileSystem.Directory.CreateDirectory(MockFileSystem.Path.Combine(gitRepoDir, ".git"));

		// Create a test file inside the git repo that should be included
		CreateFile(MockFileSystem.Path.Combine("MyGitRepo", "test.txt"), "Git repo test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(11, files.Count, "Should find 11 test.txt files, including those in regular git repositories");
		Assert.IsTrue(files.Any(f => f.Contains("MyGitRepo")), "Should include files from regular git repository directories");
	}

	[TestMethod]
	public void FindFiles_HandlesMultipleSubmodules()
	{
		// Arrange - Create multiple submodules
		string submodule1Dir = MockFileSystem.Path.Combine(TestDirectory, "Submodule1");
		string submodule2Dir = MockFileSystem.Path.Combine(TestDirectory, "Submodule2");
		MockFileSystem.Directory.CreateDirectory(submodule1Dir);
		MockFileSystem.Directory.CreateDirectory(submodule2Dir);

		// Create .git files for both submodules
		MockFileSystem.File.WriteAllText(MockFileSystem.Path.Combine(submodule1Dir, ".git"), "gitdir: ../.git/modules/Submodule1");
		MockFileSystem.File.WriteAllText(MockFileSystem.Path.Combine(submodule2Dir, ".git"), "gitdir: ../.git/modules/Submodule2");

		// Create test files in both submodules
		CreateFile(MockFileSystem.Path.Combine("Submodule1", "test.txt"), "Submodule1 test file");
		CreateFile(MockFileSystem.Path.Combine("Submodule2", "test.txt"), "Submodule2 test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(10, files.Count, "Should find 10 test.txt files, excluding both submodules");
		Assert.IsFalse(files.Any(f => f.Contains("Submodule1") || f.Contains("Submodule2")),
			"Should not include files from any submodule directories");
	}

	[TestMethod]
	public void FindFiles_SkipsNestedSubmodules()
	{
		// Arrange - Create a submodule within a regular directory
		string parentDir = MockFileSystem.Path.Combine(TestDirectory, "ParentDir");
		string nestedSubmoduleDir = MockFileSystem.Path.Combine(parentDir, "NestedSubmodule");
		MockFileSystem.Directory.CreateDirectory(nestedSubmoduleDir);

		// Create a .git file for the nested submodule
		MockFileSystem.File.WriteAllText(MockFileSystem.Path.Combine(nestedSubmoduleDir, ".git"), "gitdir: ../../.git/modules/NestedSubmodule");

		// Create test files
		CreateFile(MockFileSystem.Path.Combine("ParentDir", "test.txt"), "Parent directory test file");
		CreateFile(MockFileSystem.Path.Combine("ParentDir", "NestedSubmodule", "test.txt"), "Nested submodule test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(11, files.Count, "Should find 11 test.txt files, including parent dir but excluding nested submodule");
		Assert.IsTrue(files.Any(f => f.Contains(MockFileSystem.Path.Combine("ParentDir", "test.txt"))), "Should include files from parent directory");
		Assert.IsFalse(files.Any(f => f.Contains("NestedSubmodule")), "Should not include files from nested submodule");
	}

	[TestMethod]
	public void FindFiles_WithSearchPaths_ReturnsFilesFromSpecifiedPaths()
	{
		// Arrange
		string searchPath1 = MockFileSystem.Path.Combine(TestDirectory, "SearchPath1");
		string searchPath2 = MockFileSystem.Path.Combine(TestDirectory, "SearchPath2");
		IReadOnlyCollection<string> searchPaths = [searchPath1, searchPath2];
		IReadOnlyCollection<string> exclusionPatterns = [];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(2, files.Count, "Should find 2 test.txt files from specified search paths");
		Assert.IsTrue(files.Any(f => f.Contains("SearchPath1")), "Should include files from SearchPath1");
		Assert.IsTrue(files.Any(f => f.Contains("SearchPath2")), "Should include files from SearchPath2");
		Assert.IsFalse(files.Any(f => f.Contains("Subdir1") || f.Contains("Subdir2")), "Should not include files from other directories");
	}

	[TestMethod]
	public void FindFiles_WithEmptySearchPaths_UsesRootDirectory()
	{
		// Arrange
		IReadOnlyCollection<string> searchPaths = [];
		IReadOnlyCollection<string> exclusionPatterns = [];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(10, files.Count, "Should find all 10 test.txt files when using root directory as fallback");
	}

	[TestMethod]
	public void FindFiles_WithExclusionPatterns_ExcludesMatchingPaths()
	{
		// Arrange
		IReadOnlyCollection<string> searchPaths = [];
		IReadOnlyCollection<string> exclusionPatterns = ["*/bin/*", "*/obj/*"];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(8, files.Count, "Should find 8 test.txt files, excluding bin and obj directories");
		Assert.IsFalse(files.Any(f => f.Contains("bin") || f.Contains("obj")), "Should not include files from excluded directories");
	}

	[TestMethod]
	public void FindFiles_WithWildcardExclusion_ExcludesMatchingPaths()
	{
		// Arrange
		IReadOnlyCollection<string> searchPaths = [];
		IReadOnlyCollection<string> exclusionPatterns = ["temp*", "*node_modules*"];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(8, files.Count, "Should find 8 test.txt files, excluding temp and node_modules directories");
		Assert.IsFalse(files.Any(f => f.Contains("temp") || f.Contains("node_modules")), "Should not include files from excluded directories");
	}

	[TestMethod]
	public void FindFiles_WithNonExistentSearchPath_SkipsNonExistentPaths()
	{
		// Arrange
		string existingSearchPath = MockFileSystem.Path.Combine(TestDirectory, "SearchPath1");
		string nonExistentSearchPath = MockFileSystem.Path.Combine(TestDirectory, "NonExistentPath");
		IReadOnlyCollection<string> searchPaths = [existingSearchPath, nonExistentSearchPath];
		IReadOnlyCollection<string> exclusionPatterns = [];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(1, files.Count, "Should find 1 test.txt file from existing search path only");
		Assert.IsTrue(files.Any(f => f.Contains("SearchPath1")), "Should include files from existing search path");
	}

	[TestMethod]
	public void FindFiles_WithComplexExclusionPattern_ExcludesCorrectly()
	{
		// Arrange
		IReadOnlyCollection<string> searchPaths = [];
		IReadOnlyCollection<string> exclusionPatterns = ["*/bin/*", "*/obj/*", "temp*", "*node_modules*"];

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(searchPaths, TestDirectory, "test.txt", exclusionPatterns);

		// Assert
		Assert.AreEqual(6, files.Count, "Should find 6 test.txt files, excluding all specified patterns");
		Assert.IsFalse(files.Any(f => f.Contains("bin") || f.Contains("obj") || f.Contains("temp") || f.Contains("node_modules")),
			"Should not include files from any excluded directories");
	}
}
