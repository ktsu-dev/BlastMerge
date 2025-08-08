// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using ktsu.BlastMerge.Models;

/// <summary>
/// Interface for file diffing operations to support mocking.
/// </summary>
public interface IFileDiffer
{
	/// <summary>
	/// Finds differences between two files.
	/// </summary>
	/// <param name="file1Path">The path to the first file.</param>
	/// <param name="file2Path">The path to the second file.</param>
	/// <returns>A read-only collection of difference strings.</returns>
	public ReadOnlyCollection<string> FindDifferences(string file1Path, string file2Path);

	/// <summary>
	/// Generates a Git-style diff between two files.
	/// </summary>
	/// <param name="file1Path">The path to the first file.</param>
	/// <param name="file2Path">The path to the second file.</param>
	/// <returns>A Git-style diff as a string.</returns>
	public string GenerateGitStyleDiff(string file1Path, string file2Path);

	/// <summary>
	/// Groups files by their content hash.
	/// </summary>
	public IReadOnlyCollection<FileGroup> GroupFilesByHash(IEnumerable<string> filePaths);

	/// <summary>
	/// Groups files by filename and then by content hash.
	/// </summary>
	public IReadOnlyCollection<FileGroup> GroupFilesByFilenameAndHash(IEnumerable<string> filePaths);

	/// <summary>
	/// Groups files by content hash only (legacy behavior).
	/// </summary>
	public IReadOnlyCollection<FileGroup> GroupFilesByHashOnly(IEnumerable<string> filePaths);

	/// <summary>
	/// Generates a colored diff summary between two files.
	/// </summary>
	public Collection<ColoredDiffLine> GenerateColoredDiff(string file1Path, string file2Path);

	/// <summary>
	/// Synchronizes the content of one file to another.
	/// </summary>
	public void SyncFile(string sourcePath, string destinationPath);

	/// <summary>
	/// Finds similar files from a collection that meet the minimum similarity threshold.
	/// Only compares files with the same filename.
	/// </summary>
	public IReadOnlyCollection<FileSimilarity> FindMostSimilarFiles(IEnumerable<string> filePaths, double minimumSimilarityThreshold);
}
