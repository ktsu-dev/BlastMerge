// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Linq;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileFinderTests : MockFileSystemTestBase
{
	private FileFinderAdapter _fileFinderAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Create test files with the same name in different directories
		CreateFile(Path.Combine("test.txt"), "Root test file");
		CreateFile(Path.Combine("Subdir1", "test.txt"), "Subdir1 test file");
		CreateFile(Path.Combine("Subdir2", "test.txt"), "Subdir2 test file");
		CreateFile(Path.Combine("Subdir1", "NestedSubdir", "test.txt"), "Nested test file");

		// Create some files with different names
		CreateFile(Path.Combine("other.txt"), "Other file");
		CreateFile(Path.Combine("Subdir1", "different.txt"), "Different file");

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
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(Path.Combine(TestDirectory, "NonExistentDir"), "test.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent directory");
	}

	[TestMethod]
	public void FindFiles_WithWildcard_ReturnsAllMatches()
	{
		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "*.txt");

		// Assert
		Assert.AreEqual(6, files.Count, "Should find all 6 .txt files");
	}

	[TestMethod]
	public void FindFiles_SkipsGitSubmodules()
	{
		// Arrange - Create a submodule structure
		string submoduleDir = Path.Combine(TestDirectory, "MySubmodule");
		MockFileSystem.Directory.CreateDirectory(submoduleDir);

		// Create a .git file (not directory) which indicates this is a submodule
		MockFileSystem.File.WriteAllText(Path.Combine(submoduleDir, ".git"), "gitdir: ../.git/modules/MySubmodule");

		// Create a test file inside the submodule that should be skipped
		CreateFile(Path.Combine("MySubmodule", "test.txt"), "Submodule test file");

		// Create another file in a nested directory within the submodule
		CreateFile(Path.Combine("MySubmodule", "SubDir", "test.txt"), "Nested submodule test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(4, files.Count, "Should find 4 test.txt files, excluding those in submodules");
		Assert.IsFalse(files.Any(f => f.Contains("MySubmodule")), "Should not include files from submodule directories");
	}

	[TestMethod]
	public void FindFiles_IncludesRegularGitRepositories()
	{
		// Arrange - Create a regular git repository (not a submodule)
		string gitRepoDir = Path.Combine(TestDirectory, "MyGitRepo");
		MockFileSystem.Directory.CreateDirectory(gitRepoDir);

		// Create a .git directory (not file) which indicates this is a regular git repo, not a submodule
		MockFileSystem.Directory.CreateDirectory(Path.Combine(gitRepoDir, ".git"));

		// Create a test file inside the git repo that should be included
		CreateFile(Path.Combine("MyGitRepo", "test.txt"), "Git repo test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(5, files.Count, "Should find 5 test.txt files, including those in regular git repositories");
		Assert.IsTrue(files.Any(f => f.Contains("MyGitRepo")), "Should include files from regular git repository directories");
	}

	[TestMethod]
	public void FindFiles_HandlesMultipleSubmodules()
	{
		// Arrange - Create multiple submodules
		string submodule1Dir = Path.Combine(TestDirectory, "Submodule1");
		string submodule2Dir = Path.Combine(TestDirectory, "Submodule2");
		MockFileSystem.Directory.CreateDirectory(submodule1Dir);
		MockFileSystem.Directory.CreateDirectory(submodule2Dir);

		// Create .git files for both submodules
		MockFileSystem.File.WriteAllText(Path.Combine(submodule1Dir, ".git"), "gitdir: ../.git/modules/Submodule1");
		MockFileSystem.File.WriteAllText(Path.Combine(submodule2Dir, ".git"), "gitdir: ../.git/modules/Submodule2");

		// Create test files in both submodules
		CreateFile(Path.Combine("Submodule1", "test.txt"), "Submodule1 test file");
		CreateFile(Path.Combine("Submodule2", "test.txt"), "Submodule2 test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(4, files.Count, "Should find 4 test.txt files, excluding both submodules");
		Assert.IsFalse(files.Any(f => f.Contains("Submodule1") || f.Contains("Submodule2")),
			"Should not include files from any submodule directories");
	}

	[TestMethod]
	public void FindFiles_SkipsNestedSubmodules()
	{
		// Arrange - Create a submodule within a regular directory
		string parentDir = Path.Combine(TestDirectory, "ParentDir");
		string nestedSubmoduleDir = Path.Combine(parentDir, "NestedSubmodule");
		MockFileSystem.Directory.CreateDirectory(nestedSubmoduleDir);

		// Create a .git file for the nested submodule
		MockFileSystem.File.WriteAllText(Path.Combine(nestedSubmoduleDir, ".git"), "gitdir: ../../.git/modules/NestedSubmodule");

		// Create test files
		CreateFile(Path.Combine("ParentDir", "test.txt"), "Parent directory test file");
		CreateFile(Path.Combine("ParentDir", "NestedSubmodule", "test.txt"), "Nested submodule test file");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(5, files.Count, "Should find 5 test.txt files, including parent dir but excluding nested submodule");
		Assert.IsTrue(files.Any(f => f.Contains(Path.Combine("ParentDir", "test.txt"))), "Should include files from parent directory");
		Assert.IsFalse(files.Any(f => f.Contains("NestedSubmodule")), "Should not include files from nested submodule");
	}
}
