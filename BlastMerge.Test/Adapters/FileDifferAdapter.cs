// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;

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
		string[] lines1 = _fileSystem.File.ReadAllLines(file1Path);
		string[] lines2 = _fileSystem.File.ReadAllLines(file2Path);

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
		string[] lines1 = _fileSystem.File.ReadAllLines(file1Path);
		string[] lines2 = _fileSystem.File.ReadAllLines(file2Path);

		// Create temporary files
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			return FileDiffer.GenerateGitStyleDiff(tempFile1, tempFile2);
		}
		finally
		{
			// Clean up temporary files
			SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);
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
		string[] lines1 = _fileSystem.File.ReadAllLines(file1Path);
		string[] lines2 = _fileSystem.File.ReadAllLines(file2Path);

		// Create temporary files
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			return FileDiffer.GenerateColoredDiff(tempFile1, tempFile2, lines1, lines2);
		}
		finally
		{
			// Clean up temporary files
			SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);
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
		string? destinationDirectory = _fileSystem.Path.GetDirectoryName(destinationPath);
		if (!string.IsNullOrEmpty(destinationDirectory) && !_fileSystem.Directory.Exists(destinationDirectory))
		{
			_fileSystem.Directory.CreateDirectory(destinationDirectory);
		}

		string sourceContent = _fileSystem.File.ReadAllText(sourcePath);
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

		Dictionary<string, FileGroup> groups = [];
		FileHasherAdapter fileHasher = new(_fileSystem);

		foreach (string filePath in filePaths)
		{
			string hash = fileHasher.ComputeFileHash(filePath);

			if (!groups.TryGetValue(hash, out FileGroup? group))
			{
				group = new FileGroup { Hash = hash };
				groups[hash] = group;
			}

			group.AddFilePath(filePath);
		}

		return [.. groups.Values];
	}

	/// <summary>
	/// Groups files by their filename (without path) first, then by content hash within each filename group.
	/// This prevents files with different names from being compared/merged together.
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <returns>A collection of file groups where each group contains files with the same name and identical content</returns>
	public IReadOnlyCollection<FileGroup> GroupFilesByFilenameAndHash(IReadOnlyCollection<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		// First group by filename (basename without path)
		Dictionary<string, List<string>> filenameGroups = [];

		foreach (string filePath in filePaths)
		{
			string filename = _fileSystem.Path.GetFileName(filePath);

			if (!filenameGroups.TryGetValue(filename, out List<string>? pathsWithSameName))
			{
				pathsWithSameName = [];
				filenameGroups[filename] = pathsWithSameName;
			}

			pathsWithSameName.Add(filePath);
		}

		// Then group by content hash within each filename group
		List<FileGroup> allGroups = [];
		FileHasherAdapter fileHasher = new(_fileSystem);

		foreach (KeyValuePair<string, List<string>> filenameGroup in filenameGroups)
		{
			if (filenameGroup.Value.Count == 1)
			{
				// Single file with this name - create a group for it
				string hash = fileHasher.ComputeFileHash(filenameGroup.Value[0]);
				FileGroup group = new() { Hash = hash };
				group.AddFilePath(filenameGroup.Value[0]);
				allGroups.Add(group);
			}
			else
			{
				// Multiple files with same name - group by content hash
				Dictionary<string, FileGroup> hashGroups = [];

				foreach (string filePath in filenameGroup.Value)
				{
					string hash = fileHasher.ComputeFileHash(filePath);

					if (!hashGroups.TryGetValue(hash, out FileGroup? group))
					{
						group = new FileGroup { Hash = hash };
						hashGroups[hash] = group;
					}

					group.AddFilePath(filePath);
				}

				allGroups.AddRange(hashGroups.Values);
			}
		}

		return allGroups.AsReadOnly();
	}

	/// <summary>
	/// Groups files by content hash only (legacy behavior - may group files with different names).
	/// Use GroupFilesByFilenameAndHash for safer grouping that prevents unrelated files from being merged.
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <returns>A collection of file groups where each group contains identical files (regardless of filename)</returns>
	public IReadOnlyCollection<FileGroup> GroupFilesByHashOnly(IReadOnlyCollection<string> filePaths) =>
		GroupFilesByHash(filePaths);

	/// <summary>
	/// Finds the two most similar files from a collection of unique file groups.
	/// Only compares files with the same filename to prevent merging unrelated files.
	/// </summary>
	/// <param name="fileGroups">Collection of file groups with different content</param>
	/// <returns>A FileSimilarity object with the most similar pair, or null if less than 2 groups</returns>
	public FileSimilarity? FindMostSimilarFiles(IReadOnlyCollection<FileGroup> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);

		if (fileGroups.Count < 2)
		{
			return null;
		}

		List<FileGroup> groups = [.. fileGroups];
		FileSimilarity? mostSimilar = null;
		double highestSimilarity = -1.0;

		for (int i = 0; i < groups.Count; i++)
		{
			for (int j = i + 1; j < groups.Count; j++)
			{
				string file1 = groups[i].FilePaths.First();
				string file2 = groups[j].FilePaths.First();

				// CRITICAL SAFETY CHECK: Only compare files with the same filename
				// This prevents merging unrelated files like update-readme.yml with dotnet-sdk.yml
				string filename1 = _fileSystem.Path.GetFileName(file1);
				string filename2 = _fileSystem.Path.GetFileName(file2);

				if (!string.Equals(filename1, filename2, StringComparison.OrdinalIgnoreCase))
				{
					continue; // Skip comparison of files with different names
				}

				double similarity = CalculateFileSimilarity(file1, file2);

				if (similarity > highestSimilarity)
				{
					highestSimilarity = similarity;
					mostSimilar = new FileSimilarity(file1, file2, similarity);
				}
			}
		}

		return mostSimilar;
	}

	/// <summary>
	/// Calculates similarity between two files using the mock filesystem
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>Similarity score between 0.0 and 1.0</returns>
	private double CalculateFileSimilarity(string file1, string file2)
	{
		string[] lines1 = _fileSystem.File.ReadAllLines(file1);
		string[] lines2 = _fileSystem.File.ReadAllLines(file2);

		// Use FileDiffer.CalculateLineSimilarity which doesn't require file access
		return FileDiffer.CalculateLineSimilarity(lines1, lines2);
	}

	/// <summary>
	/// Internal implementation of FindDifferences that works with string arrays
	/// </summary>
	/// <param name="lines1">Lines from the first file</param>
	/// <param name="lines2">Lines from the second file</param>
	/// <returns>Collection of line differences</returns>
	private static ReadOnlyCollection<LineDifference> FindDifferencesInternal(string[] lines1, string[] lines2)
	{
		List<LineDifference> differences = [];

		// Simple line-by-line comparison for testing purposes
		int maxLines = Math.Max(lines1.Length, lines2.Length);

		for (int i = 0; i < maxLines; i++)
		{
			string? line1 = i < lines1.Length ? lines1[i] : null;
			string? line2 = i < lines2.Length ? lines2[i] : null;

			if (line1 != line2)
			{
				differences.Add(new LineDifference(null, null, null, null, default)
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
