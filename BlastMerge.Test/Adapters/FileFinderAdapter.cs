// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

/// <summary>
/// Adapter for FileFinder that works with a mock filesystem
/// </summary>
/// <remarks>
/// Initializes a new instance of the FileFinderAdapter class
/// </remarks>
/// <param name="fileSystem">The file system to use</param>
public class FileFinderAdapter(IFileSystem fileSystem)
{
	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

	/// <summary>
	/// Recursively finds all files with the specified filename
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <returns>A list of full file paths</returns>
	public IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName)
	{
		List<string> result = [];

		try
		{
			// Search in current directory
			string[] filesInCurrentDir = _fileSystem.Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);
			result.AddRange(filesInCurrentDir);

			// Search in subdirectories
			foreach (string directory in _fileSystem.Directory.GetDirectories(rootDirectory))
			{
				try
				{
					// Skip git submodules
					if (IsGitSubmodule(directory))
					{
						continue;
					}

					IReadOnlyCollection<string> filesInSubDir = FindFiles(directory, fileName);
					result.AddRange(filesInSubDir);
				}
				catch (UnauthorizedAccessException)
				{
					// Skip directories we don't have access to
				}
				catch (DirectoryNotFoundException)
				{
					// Skip directories that may have been deleted during enumeration
				}
			}
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException
								or DirectoryNotFoundException
								or IOException)
		{
			// Log or handle exception as needed
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Determines if a directory is a git submodule
	/// </summary>
	/// <param name="directoryPath">The directory path to check</param>
	/// <returns>True if the directory is a git submodule, false otherwise</returns>
	private bool IsGitSubmodule(string directoryPath)
	{
		try
		{
			string gitPath = _fileSystem.Path.Combine(directoryPath, ".git");

			// Git submodules have a .git file (not directory) that contains a reference
			// to the actual git directory location
			return _fileSystem.File.Exists(gitPath) && !_fileSystem.Directory.Exists(gitPath);
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException
								or DirectoryNotFoundException
								or IOException
								or ArgumentException
								or PathTooLongException)
		{
			// If we can't access the directory or path is invalid, assume it's not a submodule
			return false;
		}
	}
}
