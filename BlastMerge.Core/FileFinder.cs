// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Finds files matching a specific name in a directory hierarchy
/// </summary>
public static class FileFinder
{
	/// <summary>
	/// Recursively finds all files with the specified filename
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName)
	{
		List<string> result = [];

		try
		{
			// Search in current directory
			string[] filesInCurrentDir = Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);
			result.AddRange(filesInCurrentDir);

			// Search in subdirectories
			foreach (string directory in Directory.GetDirectories(rootDirectory))
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
		catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
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
	private static bool IsGitSubmodule(string directoryPath)
	{
		try
		{
			string gitPath = Path.Combine(directoryPath, ".git");

			// Git submodules have a .git file (not directory) that contains a reference
			// to the actual git directory location
			return File.Exists(gitPath) && !Directory.Exists(gitPath);
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
