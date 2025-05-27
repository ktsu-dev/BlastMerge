// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test.Adapters;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

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
	public IReadOnlyCollection<FileDiffer.Difference> FindDifferences(string file1Path, string file2Path)
	{
		if (!_fileSystem.File.Exists(file1Path))
		{
			throw new FileNotFoundException("File not found", file1Path);
		}

		if (!_fileSystem.File.Exists(file2Path))
		{
			throw new FileNotFoundException("File not found", file2Path);
		}

		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		return FileDiffer.FindDifferences(lines1, lines2);
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

		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		return FileDiffer.GenerateGitStyleDiff(file1Path, file2Path, lines1, lines2);
	}

	/// <summary>
	/// Generates a colored diff between two files
	/// </summary>
	/// <param name="file1Path">Path to the first file</param>
	/// <param name="file2Path">Path to the second file</param>
	/// <returns>Collection of colored diff lines</returns>
	public IReadOnlyCollection<FileDiffer.ColoredLine> GenerateColoredDiff(string file1Path, string file2Path)
	{
		if (!_fileSystem.File.Exists(file1Path))
		{
			throw new FileNotFoundException("File not found", file1Path);
		}

		if (!_fileSystem.File.Exists(file2Path))
		{
			throw new FileNotFoundException("File not found", file2Path);
		}

		var lines1 = _fileSystem.File.ReadAllLines(file1Path);
		var lines2 = _fileSystem.File.ReadAllLines(file2Path);

		return FileDiffer.GenerateColoredDiff(file1Path, file2Path, lines1, lines2);
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

		var sourceContent = _fileSystem.File.ReadAllText(sourcePath);
		_fileSystem.File.WriteAllText(destinationPath, sourceContent);
	}
}
