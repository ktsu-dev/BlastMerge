// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

/// <summary>
/// Covers the paths of <see cref="FileFinder"/> the existing suite leaves untouched: a root that does
/// not exist, a root excluded by its own exclusion pattern, and a submodule check that cannot be made.
/// </summary>
[TestClass]
public class FileFinderCoverageTests : MockFileSystemTestBase
{
	/// <summary>
	/// Searching a directory that does not exist finds nothing rather than throwing.
	/// </summary>
	[TestMethod]
	public void FindFiles_MissingRoot_ReturnsEmpty()
	{
		string missing = Path.Join(TestDirectory, "missing");

		Assert.IsEmpty(FileFinder.FindFiles(missing, "*.txt", MockFileSystem));
	}

	/// <summary>
	/// The exclusion-aware search finds nothing in a directory that does not exist, rather than throwing.
	/// </summary>
	[TestMethod]
	public void FindFiles_WithExclusions_MissingRoot_ReturnsEmpty()
	{
		string missing = Path.Join(TestDirectory, "missing");
		IReadOnlyCollection<string> exclusions = [];

		Assert.IsEmpty(FileFinder.FindFiles(missing, "*.txt", exclusions, progressCallback: null));
	}

	/// <summary>
	/// A root that matches an exclusion pattern is not searched at all.
	/// </summary>
	[TestMethod]
	public void FindFiles_WithExclusions_ExcludedRoot_ReturnsEmpty()
	{
		string root = CreateDirectory("excludedroot");
		CreateFile(Path.Join("excludedroot", "file.txt"), "x");
		CreateFile(Path.Join("excludedroot", "nested", "file.txt"), "y");
		IReadOnlyCollection<string> exclusions = ["excludedroot"];
		IReadOnlyCollection<string> noExclusions = [];
		List<string> reported = [];

		IReadOnlyCollection<string> excluded = FileFinder.FindFiles(root, "file.txt", exclusions, reported.Add);

		Assert.IsEmpty(excluded);
		Assert.IsEmpty(reported);
		Assert.HasCount(2, FileFinder.FindFiles(root, "file.txt", noExclusions, progressCallback: null));
	}

	/// <summary>
	/// When a directory's submodule marker cannot be checked, the directory is searched as an ordinary one.
	/// </summary>
	[TestMethod]
	public void FindFiles_SubmoduleCheckFails_SearchesDirectoryAnyway()
	{
		string root = Path.Join(TestDirectory, "root");
		string child = Path.Join(root, "child");
		string found = Path.Join(child, "file.txt");

		Mock<IDirectory> directory = new();
		directory.Setup(d => d.GetFiles(root, "file.txt", SearchOption.TopDirectoryOnly)).Returns([]);
		directory.Setup(d => d.GetFiles(child, "file.txt", SearchOption.TopDirectoryOnly)).Returns([found]);
		directory.Setup(d => d.GetDirectories(root)).Returns([child]);
		directory.Setup(d => d.GetDirectories(child)).Returns([]);

		Mock<IPath> path = new();
		path.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>())).Throws(new ArgumentException("bad path"));

		Mock<IFileSystem> fileSystem = new();
		fileSystem.Setup(f => f.Directory).Returns(directory.Object);
		fileSystem.Setup(f => f.Path).Returns(path.Object);

		IReadOnlyCollection<string> files = FileFinder.FindFiles(root, "file.txt", fileSystem.Object);

		Assert.AreSequenceEqual([found], files);
	}

	/// <summary>
	/// A repository's own <c>config</c> under <c>.git</c> is never found as a copy of a project's
	/// <c>config</c>, so it can't be offered for merging into another repository.
	/// </summary>
	[TestMethod]
	public void FindFiles_SkipsGitDirectories()
	{
		string root = CreateDirectory("dev");
		string configA = CreateFile(Path.Join("dev", "a", "config"), "x");
		string configB = CreateFile(Path.Join("dev", "b", "config"), "x");
		CreateFile(Path.Join("dev", "a", ".git", "config"), "[remote \"origin\"]\n\turl = https://example.com/a.git");
		CreateFile(Path.Join("dev", "b", ".git", "config"), "[remote \"origin\"]\n\turl = https://example.com/b.git");
		CreateFile(Path.Join("dev", "b", ".git", "modules", "sub", "config"), "y");

		IReadOnlyCollection<string> files = FileFinder.FindFiles(root, "config", MockFileSystem);

		CollectionAssert.AreEquivalent(new[] { configA, configB }, files.ToArray());
	}

	/// <summary>
	/// The exclusion-aware search skips <c>.git</c> and other version control metadata directories
	/// even when no exclusion pattern names them.
	/// </summary>
	[TestMethod]
	public void FindFiles_WithExclusions_SkipsVersionControlDirectories()
	{
		string root = CreateDirectory("repos");
		string kept = CreateFile(Path.Join("repos", "a", "HEAD"), "x");
		CreateFile(Path.Join("repos", "a", ".git", "HEAD"), "ref: refs/heads/main");
		CreateFile(Path.Join("repos", "b", ".hg", "HEAD"), "y");
		CreateFile(Path.Join("repos", "c", ".svn", "HEAD"), "y");
		IReadOnlyCollection<string> noExclusions = [];

		IReadOnlyCollection<string> files = FileFinder.FindFiles(root, "HEAD", noExclusions, progressCallback: null);

		Assert.AreSequenceEqual([kept], files);
	}
}
