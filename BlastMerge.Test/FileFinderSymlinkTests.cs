// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Directory symlinks in the searched tree, run against the real file system so the links are real (#65).
/// </summary>
[TestClass]
public class FileFinderSymlinkTests
{
	private string root = null!;

	[TestInitialize]
	public void Setup() => root = Directory.CreateTempSubdirectory("blastmerge-symlink-").FullName;

	[TestCleanup]
	public void Cleanup() => Directory.Delete(root, recursive: true);

	/// <summary>
	/// Creates a directory symlink, or marks the test inconclusive where the platform won't allow it
	/// (Windows without Developer Mode or elevation).
	/// </summary>
	private static void Link(string path, string target)
	{
		try
		{
			Directory.CreateSymbolicLink(path, target);
		}
		catch (IOException ex)
		{
			Assert.Inconclusive($"Cannot create a directory symlink here: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			Assert.Inconclusive($"Cannot create a directory symlink here: {ex.Message}");
		}
	}

	/// <summary>
	/// The layout a pnpm or yarn workspace leaves when two packages depend on each other.
	/// </summary>
	private string CreateWorkspaceCycle()
	{
		string pkgs = Path.Join(root, "pkgs");
		foreach (string name in new[] { "a", "b" })
		{
			Directory.CreateDirectory(Path.Join(pkgs, name, "node_modules"));
			File.WriteAllText(Path.Join(pkgs, name, "README.md"), name);
		}

		Link(Path.Join(pkgs, "a", "node_modules", "b"), Path.Join("..", "..", "b"));
		Link(Path.Join(pkgs, "b", "node_modules", "a"), Path.Join("..", "..", "a"));
		return pkgs;
	}

	/// <summary>
	/// A tree whose links point back at itself and its parent.
	/// </summary>
	private string CreateSelfAndParentLinks()
	{
		string tree = Path.Join(root, "tree2");
		Directory.CreateDirectory(Path.Join(tree, "x"));
		File.WriteAllText(Path.Join(tree, "README.md"), "only copy");
		Link(Path.Join(tree, "x", "up"), "..");
		Link(Path.Join(tree, "x", "self"), ".");
		return tree;
	}

	[TestMethod]
	public void FindFiles_WithExclusions_WorkspaceCycle_ReturnsEachRealFileOnce()
	{
		string pkgs = CreateWorkspaceCycle();

		IReadOnlyCollection<string> files = FileFinder.FindFiles([], pkgs, "README.md", [], new FileSystem());

		CollectionAssert.AreEquivalent(
			new[] { Path.Join(pkgs, "a", "README.md"), Path.Join(pkgs, "b", "README.md") },
			files.ToArray(),
			string.Join(", ", files));
	}

	[TestMethod]
	public void FindFiles_WorkspaceCycle_ReturnsEachRealFileOnce()
	{
		string pkgs = CreateWorkspaceCycle();

		IReadOnlyCollection<string> files = FileFinder.FindFiles(pkgs, "README.md", new FileSystem());

		CollectionAssert.AreEquivalent(
			new[] { Path.Join(pkgs, "a", "README.md"), Path.Join(pkgs, "b", "README.md") },
			files.ToArray(),
			string.Join(", ", files));
	}

	/// <summary>
	/// Runs a search that may never finish without the fix, failing rather than hanging the test run.
	/// </summary>
	private static IReadOnlyCollection<string> WithinTenSeconds(Func<IReadOnlyCollection<string>> search)
	{
		Task<IReadOnlyCollection<string>> task = Task.Run(search);
		Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10)), "The search did not finish within 10 seconds");
		return task.Result;
	}

	[TestMethod]
	public void FindFiles_WithExclusions_SelfAndParentLinks_Terminates()
	{
		string tree = CreateSelfAndParentLinks();

		IReadOnlyCollection<string> files = WithinTenSeconds(() => FileFinder.FindFiles([], tree, "README.md", [], new FileSystem()));

		Assert.AreEqual(Path.Join(tree, "README.md"), files.Single());
	}

	[TestMethod]
	public void FindFiles_SelfAndParentLinks_Terminates()
	{
		string tree = CreateSelfAndParentLinks();

		IReadOnlyCollection<string> files = WithinTenSeconds(() => FileFinder.FindFiles(tree, "README.md", new FileSystem()));

		Assert.AreEqual(Path.Join(tree, "README.md"), files.Single());
	}
}
