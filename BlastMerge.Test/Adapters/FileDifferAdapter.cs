// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;

/// <summary>
/// Test adapter for FileDiffer service - wraps the service to implement the test interface
/// </summary>
/// <param name="fileDiffer">The FileDiffer service instance</param>
public class FileDifferAdapter(FileDiffer fileDiffer) : IFileDiffer
{
	/// <summary>
	/// Finds differences between two files using FileDiffer
	/// </summary>
	/// <param name="file1Path">Path to first file</param>
	/// <param name="file2Path">Path to second file</param>
	/// <returns>Collection of difference strings</returns>
	public ReadOnlyCollection<string> FindDifferences(string file1Path, string file2Path)
	{
		ArgumentNullException.ThrowIfNull(file1Path);
		ArgumentNullException.ThrowIfNull(file2Path);

		// Use FileDiffer to find differences and convert to string collection
		IReadOnlyCollection<LineDifference> differences = fileDiffer.FindDifferences(file1Path, file2Path);
		List<string> stringDifferences = [.. differences.Select(d =>
			$"Line {d.LineNumber1}: {d.Content1} -> Line {d.LineNumber2}: {d.Content2}")];

		return stringDifferences.AsReadOnly();
	}

	/// <summary>
	/// Generates a Git-style diff between two files using FileDiffer
	/// </summary>
	/// <param name="file1Path">Path to first file</param>
	/// <param name="file2Path">Path to second file</param>
	/// <returns>Git-style diff string</returns>
	public string GenerateGitStyleDiff(string file1Path, string file2Path)
	{
		ArgumentNullException.ThrowIfNull(file1Path);
		ArgumentNullException.ThrowIfNull(file2Path);

		// Use FileDiffer instance method
		return fileDiffer.GenerateGitStyleDiff(file1Path, file2Path);
	}

	public IReadOnlyCollection<FileGroup> GroupFilesByHash(IEnumerable<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		List<string> paths = [.. filePaths];
		return fileDiffer.GroupFilesByHash(paths);
	}

	public IReadOnlyCollection<FileGroup> GroupFilesByFilenameAndHash(IEnumerable<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		List<string> paths = [.. filePaths];
		return fileDiffer.GroupFilesByFilenameAndHash(paths);
	}

	public IReadOnlyCollection<FileGroup> GroupFilesByHashOnly(IEnumerable<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		List<string> paths = [.. filePaths];
		return fileDiffer.GroupFilesByHashOnly(paths);
	}

	public Collection<ColoredDiffLine> GenerateColoredDiff(string file1Path, string file2Path)
	{
		ArgumentNullException.ThrowIfNull(file1Path);
		ArgumentNullException.ThrowIfNull(file2Path);

		// Directly use FileDiffer's colored diff implementation
		return fileDiffer.GenerateColoredDiff(file1Path, file2Path, [], []);
	}

	public void SyncFile(string sourcePath, string destinationPath)
	{
		ArgumentNullException.ThrowIfNull(sourcePath);
		ArgumentNullException.ThrowIfNull(destinationPath);
		fileDiffer.SyncFile(sourcePath, destinationPath);
	}

	public IReadOnlyCollection<FileSimilarity> FindMostSimilarFiles(IEnumerable<string> filePaths, double minimumSimilarityThreshold)
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		// Convert file paths to FileGroups by filename first, then compute hash-only groups
		// to match the intended behavior of comparing within same filename
		IReadOnlyCollection<FileGroup> groups = fileDiffer.GroupFilesByFilenameAndHash([.. filePaths]);
		// Find best similarity among groups (API expects groups)
		FileSimilarity? best = fileDiffer.FindMostSimilarFiles(groups);
		return best is null ? [] : new[] { best };
	}
}
