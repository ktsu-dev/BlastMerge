// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

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

		return new System.Collections.ObjectModel.ReadOnlyCollection<string>(result);
	}

	/// <summary>
	/// Finds all files matching the specified filename across multiple search paths with exclusion support
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <returns>A list of full file paths</returns>
	public IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns)
	{
		ArgumentNullException.ThrowIfNull(searchPaths);
		ArgumentNullException.ThrowIfNull(rootDirectory);
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(pathExclusionPatterns);

		List<string> result = [];

		// Use search paths if provided, otherwise use the root directory
		IEnumerable<string> directoriesToSearch = searchPaths.Count > 0 ? searchPaths : [rootDirectory];

		foreach (string searchPath in directoriesToSearch)
		{
			if (_fileSystem.Directory.Exists(searchPath))
			{
				System.Collections.ObjectModel.ReadOnlyCollection<string> filesInPath = FindFilesWithExclusions(searchPath, fileName, pathExclusionPatterns);
				result.AddRange(filesInPath);
			}
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Recursively finds all files with the specified filename, applying exclusion patterns
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <returns>A list of full file paths</returns>
	private System.Collections.ObjectModel.ReadOnlyCollection<string> FindFilesWithExclusions(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns)
	{
		List<string> result = [];

		try
		{
			// Check if this directory should be excluded
			if (ShouldExcludePath(rootDirectory, pathExclusionPatterns))
			{
				return new System.Collections.ObjectModel.ReadOnlyCollection<string>(result);
			}

			// Add files from current directory
			AddFilesFromCurrentDirectory(result, rootDirectory, fileName, pathExclusionPatterns);

			// Add files from subdirectories
			AddFilesFromSubdirectories(result, rootDirectory, fileName, pathExclusionPatterns);
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
		{
			// Log or handle exception as needed
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Adds files from the current directory to the result list
	/// </summary>
	private void AddFilesFromCurrentDirectory(
		List<string> result,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns)
	{
		string[] filesInCurrentDir = _fileSystem.Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);

		foreach (string file in filesInCurrentDir)
		{
			if (!ShouldExcludePath(file, pathExclusionPatterns))
			{
				result.Add(file);
			}
		}
	}

	/// <summary>
	/// Adds files from subdirectories to the result list
	/// </summary>
	private void AddFilesFromSubdirectories(
		List<string> result,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns)
	{
		foreach (string directory in _fileSystem.Directory.GetDirectories(rootDirectory))
		{
			try
			{
				// Skip git submodules and excluded directories
				if (IsGitSubmodule(directory) || ShouldExcludePath(directory, pathExclusionPatterns))
				{
					continue;
				}

				System.Collections.ObjectModel.ReadOnlyCollection<string> filesInSubDir = FindFilesWithExclusions(directory, fileName, pathExclusionPatterns);
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

	/// <summary>
	/// Determines if a path should be excluded based on exclusion patterns
	/// </summary>
	/// <param name="path">The path to check</param>
	/// <param name="exclusionPatterns">The exclusion patterns to match against</param>
	/// <returns>True if the path should be excluded, false otherwise</returns>
	private bool ShouldExcludePath(string path, IReadOnlyCollection<string> exclusionPatterns)
	{
		if (exclusionPatterns.Count == 0)
		{
			return false;
		}

		string normalizedPath = _fileSystem.Path.GetFullPath(path).Replace(_fileSystem.Path.DirectorySeparatorChar, '/');

		foreach (string pattern in exclusionPatterns)
		{
			if (string.IsNullOrWhiteSpace(pattern))
			{
				continue;
			}

			// Support simple glob patterns
			string normalizedPattern = pattern.Replace('\\', '/');

			// Convert simple glob patterns to regex-like matching
			if (normalizedPattern.Contains('*') || normalizedPattern.Contains('?'))
			{
				if (MatchesGlobPattern(normalizedPath, normalizedPattern))
				{
					return true;
				}
			}
			else
			{
				// Exact match or contains check
				if (normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a path matches a simple glob pattern
	/// </summary>
	/// <param name="path">The path to check</param>
	/// <param name="pattern">The glob pattern</param>
	/// <returns>True if the path matches the pattern, false otherwise</returns>
	private static bool MatchesGlobPattern(string path, string pattern)
	{
		if (pattern.StartsWith("*/") && pattern.EndsWith("/*"))
		{
			return MatchesDirectoryPattern(path, pattern);
		}

		if (pattern.StartsWith('*') && pattern.EndsWith('*') && !pattern.Contains('/'))
		{
			return MatchesSubstringPattern(path, pattern);
		}

		if (pattern.EndsWith('*') && !pattern.Contains('/'))
		{
			return MatchesPrefixPattern(path, pattern);
		}

		return MatchesRegexPattern(path, pattern);
	}

	private static bool MatchesDirectoryPattern(string path, string pattern)
	{
		// Pattern like "*/bin/*" - check if path contains the directory
		string dirName = pattern[2..^2]; // Remove "*/" and "/*"
		return path.Contains($"/{dirName}/", StringComparison.OrdinalIgnoreCase);
	}

	private static bool MatchesSubstringPattern(string path, string pattern)
	{
		// Pattern like "*node_modules*" - check if any path component contains the substring
		string substring = pattern[1..^1]; // Remove leading and trailing "*"
		string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return pathComponents.Any(component => component.Contains(substring, StringComparison.OrdinalIgnoreCase));
	}

	private static bool MatchesPrefixPattern(string path, string pattern)
	{
		// Pattern like "temp*" - check if any path component starts with the prefix
		string prefix = pattern[..^1]; // Remove trailing "*"
		string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return pathComponents.Any(component => component.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	private static bool MatchesRegexPattern(string path, string pattern)
	{
		// Fallback to general regex pattern matching
		string regexPattern = "^" + pattern
			.Replace("*", ".*")
			.Replace("?", ".")
			.Replace("/", "\\/") + "$";

		try
		{
			return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		}
		catch (ArgumentException)
		{
			// If regex fails, fall back to simple contains check
			return path.Contains(pattern.Replace("*", "").Replace("?", ""), StringComparison.OrdinalIgnoreCase);
		}
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
