// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

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
		var result = new List<string>();

		try
		{
			// Search in current directory
			var filesInCurrentDir = Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);
			result.AddRange(filesInCurrentDir);

			// Search in subdirectories
			foreach (var directory in Directory.GetDirectories(rootDirectory))
			{
				try
				{
					var filesInSubDir = FindFiles(directory, fileName);
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
}
