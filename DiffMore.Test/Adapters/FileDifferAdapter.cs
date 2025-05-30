// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test.Adapters;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using ktsu.DiffMore.Core;

/// <summary>
/// Adapter for FileDiffer that works with a mock filesystem
/// </summary>
/// <remarks>
/// Initializes a new instance of the FileDifferAdapter class
/// </remarks>
/// <param name="fileSystem">The file system to use</param>
public class FileDifferAdapter(IFileSystem fileSystem)
{
	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

	/// <summary>
	/// Find differences between two files
	/// </summary>
	/// <param name="file1Path">Path to the first file</param>
	/// <param name="file2Path">Path to the second file</param>
	/// <returns>Collection of differences</returns>
	public IReadOnlyCollection<LineDifference> FindDifferences(string file1Path, string file2Path)
	{
		if (!_fileSystem.File.Exists(file1Path))
		{
			throw new FileNotFoundException("File not found", file1Path);
		}

		if (!_fileSystem.File.Exists(file2Path))
		{
			throw new FileNotFoundException("File not found", file2Path);
		}

		// Since FileDiffer.FindDifferences expects file paths and uses File.ReadAllLines internally,
		// we need to create temporary files or use a different approach for mock filesystem
		// For now, let's create a simple implementation that works with our mock filesystem
		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		// We'll need to implement our own diff logic here since FileDiffer.FindDifferences
		// uses the real file system internally
		return FindDifferencesInternal(lines1, lines2);
	}

	/// <summary>
	/// Generates a Git-style diff between two files
	/// </summary>
	/// <param name="file1Path">Path to the first file</param>
	/// <param name="file2Path">Path to the second file</param>
	/// <returns>Git-style diff string</returns>
	public string GenerateGitStyleDiff(string file1Path, string file2Path)
	{
		if (!_fileSystem.File.Exists(file1Path))
		{
			throw new FileNotFoundException("File not found", file1Path);
		}

		if (!_fileSystem.File.Exists(file2Path))
		{
			throw new FileNotFoundException("File not found", file2Path);
		}

		// Since FileDiffer.GenerateGitStyleDiff expects real file paths and reads from the actual filesystem,
		// we need to create temporary files with the content from our mock filesystem
		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		// Create temporary files
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			return FileDiffer.GenerateGitStyleDiff(tempFile1, tempFile2);
		}
		finally
		{
			// Clean up temporary files
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Generates a colored diff between two files
	/// </summary>
	/// <param name="file1Path">Path to the first file</param>
	/// <param name="file2Path">Path to the second file</param>
	/// <returns>Collection of colored diff lines</returns>
	public Collection<ColoredDiffLine> GenerateColoredDiff(string file1Path, string file2Path)
	{
		if (!_fileSystem.File.Exists(file1Path))
		{
			throw new FileNotFoundException("File not found", file1Path);
		}

		if (!_fileSystem.File.Exists(file2Path))
		{
			throw new FileNotFoundException("File not found", file2Path);
		}

		// Since FileDiffer.GenerateColoredDiff calls DiffPlexDiffer.GenerateColoredDiff which expects real file paths
		// and reads from the actual filesystem, we need to create temporary files with the content from our mock filesystem
		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		// Create temporary files
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			return FileDiffer.GenerateColoredDiff(tempFile1, tempFile2, lines1, lines2);
		}
		finally
		{
			// Clean up temporary files
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Synchronizes the content of one file to another
	/// </summary>
	/// <param name="sourcePath">Path to the source file</param>
	/// <param name="destinationPath">Path to the destination file</param>
	public void SyncFile(string sourcePath, string destinationPath)
	{
		if (!_fileSystem.File.Exists(sourcePath))
		{
			throw new FileNotFoundException("Source file not found", sourcePath);
		}

		// Create destination directory if it doesn't exist
		var destinationDirectory = _fileSystem.Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(destinationDirectory) && !_fileSystem.Directory.Exists(destinationDirectory))
		{
			_fileSystem.Directory.CreateDirectory(destinationDirectory);
		}

		var sourceContent = _fileSystem.File.ReadAllText(sourcePath);
		_fileSystem.File.WriteAllText(destinationPath, sourceContent);
	}

	/// <summary>
	/// Groups files by their hash to identify unique versions
	/// </summary>
	/// <param name="filePaths">Collection of file paths to group</param>
	/// <returns>A collection of file groups where each group contains identical files</returns>
	public IReadOnlyCollection<FileGroup> GroupFilesByHash(IReadOnlyCollection<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		var groups = new Dictionary<string, FileGroup>();
		var fileHasher = new FileHasherAdapter(_fileSystem);

		foreach (var filePath in filePaths)
		{
			var hash = fileHasher.ComputeFileHash(filePath);

			if (!groups.TryGetValue(hash, out var group))
			{
				group = new FileGroup { Hash = hash };
				groups[hash] = group;
			}

			group.AddFilePath(filePath);
		}

		return [.. groups.Values];
	}

	/// <summary>
	/// Internal implementation of FindDifferences that works with string arrays
	/// </summary>
	/// <param name="lines1">Lines from the first file</param>
	/// <param name="lines2">Lines from the second file</param>
	/// <returns>Collection of line differences</returns>
	private static ReadOnlyCollection<LineDifference> FindDifferencesInternal(string[] lines1, string[] lines2)
	{
		var differences = new List<LineDifference>();

		// Simple line-by-line comparison for testing purposes
		var maxLines = Math.Max(lines1.Length, lines2.Length);

		for (var i = 0; i < maxLines; i++)
		{
			var line1 = i < lines1.Length ? lines1[i] : null;
			var line2 = i < lines2.Length ? lines2[i] : null;

			if (line1 != line2)
			{
				differences.Add(new LineDifference
				{
					LineNumber1 = line1 != null ? i + 1 : 0,
					LineNumber2 = line2 != null ? i + 1 : 0,
					Content1 = line1,
					Content2 = line2
				});
			}
		}

		return differences.AsReadOnly();
	}
}
