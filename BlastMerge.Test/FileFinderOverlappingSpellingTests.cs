// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.IO.Abstractions;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Search paths that overlap but are spelled differently, run against the real file system, since the
/// spellings only diverge in the strings a real directory enumeration returns.
/// </summary>
[TestClass]
public class FileFinderOverlappingSpellingTests
{
	private string root = null!;

	[TestInitialize]
	public void Setup()
	{
		root = Directory.CreateTempSubdirectory("blastmerge-overlap-").FullName;
		Directory.CreateDirectory(Path.Join(root, "a"));
		File.WriteAllText(Path.Join(root, "a", "README.md"), "only copy");
	}

	[TestCleanup]
	public void Cleanup() => Directory.Delete(root, recursive: true);

	[TestMethod]
	[DataRow("./a")]
	[DataRow("a/../a")]
	[DataRow("a/./")]
	public void FindFiles_SearchPathSpelledDifferently_ReturnsTheFileOnce(string overlapping)
	{
		// Arrange
		IReadOnlyCollection<string> searchPaths = [root, root + "/" + overlapping];

		// Act
		IReadOnlyCollection<string> files = FileFinder.FindFiles(searchPaths, root, "README.md", [], new FileSystem());

		// Assert
		Assert.HasCount(1, files, string.Join(", ", files));
	}

	[TestMethod]
	public void FindFiles_RelativeAndAbsoluteSearchPaths_ReturnsTheFileOnce()
	{
		// Arrange: the same folder, once relative to the current directory and once absolute
		string relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Join(root, "a"));
		IReadOnlyCollection<string> searchPaths = [root, relative];

		// Act
		IReadOnlyCollection<string> files = FileFinder.FindFiles(searchPaths, root, "README.md", [], new FileSystem());

		// Assert
		Assert.HasCount(1, files, string.Join(", ", files));
	}

	[TestMethod]
	public void FindFiles_OverlappingSpelling_KeepsTheFirstSpelling()
	{
		// Act
		IReadOnlyCollection<string> files = FileFinder.FindFiles([root, root + "/./a"], root, "README.md", [], new FileSystem());

		// Assert
		Assert.AreEqual(Path.Join(root, "a", "README.md"), files.Single());
	}

	[TestMethod]
	public void WriteToAll_SameFileSpelledTwice_WritesItOnce()
	{
		// Arrange
		string file = Path.Join(root, "a", "README.md");
		string sameFile = root + "/./a/README.md";
		CountingFileSystem fileSystem = new();

		// Act
		MergedContentWriter.WriteToAll(fileSystem, [file, sameFile], "merged", System.Text.Encoding.UTF8);

		// Assert
		Assert.AreEqual(1, fileSystem.Writes);
		Assert.AreEqual("merged", File.ReadAllText(file));
	}

	/// <summary>
	/// A real file system that counts text writes.
	/// </summary>
	private sealed class CountingFileSystem : FileSystem
	{
		private readonly CountingFile file;

		public CountingFileSystem() => file = new CountingFile(this);

		public int Writes => file.Writes;

		public override IFile File => file;
	}

	private sealed class CountingFile(IFileSystem fileSystem) : FileWrapper(fileSystem)
	{
		public int Writes { get; private set; }

		public override void WriteAllText(string path, string? contents, System.Text.Encoding encoding)
		{
			Writes++;
			base.WriteAllText(path, contents, encoding);
		}
	}
}
