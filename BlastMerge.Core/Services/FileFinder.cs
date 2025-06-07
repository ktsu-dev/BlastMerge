// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
	public static IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName) =>
		FindFiles(rootDirectory, fileName, null);

	/// <summary>
	/// Recursively finds all files with the specified filename with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName, Action<string>? progressCallback)
	{
		List<string> result = [];

		try
		{
			// Search in current directory
			string[] filesInCurrentDir = Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);
			foreach (string file in filesInCurrentDir)
			{
				result.Add(file);
				progressCallback?.Invoke(file);
			}

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

					IReadOnlyCollection<string> filesInSubDir = FindFiles(directory, fileName, progressCallback);
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

		return new ReadOnlyCollection<string>(result);
	}

	/// <summary>
	/// Finds all files matching the specified filename across multiple search paths with exclusion support
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns) =>
		FindFiles(searchPaths, rootDirectory, fileName, pathExclusionPatterns, null);

	/// <summary>
	/// Finds all files matching the specified filename across multiple search paths with exclusion support and progress reporting
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Action<string>? progressCallback)
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
			if (Directory.Exists(searchPath))
			{
				ReadOnlyCollection<string> filesInPath = FindFilesWithExclusions(searchPath, fileName, pathExclusionPatterns, progressCallback);
				result.AddRange(filesInPath);
			}
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Recursively finds all files with the specified filename, applying exclusion patterns with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Action<string>? progressCallback) =>
		FindFilesWithExclusions(rootDirectory, fileName, pathExclusionPatterns, progressCallback);

	/// <summary>
	/// Recursively finds all files with the specified filename, applying exclusion patterns with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	private static ReadOnlyCollection<string> FindFilesWithExclusions(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Action<string>? progressCallback)
	{
		List<string> result = [];

		try
		{
			// Check if this directory should be excluded
			if (ShouldExcludePath(rootDirectory, pathExclusionPatterns))
			{
				return new ReadOnlyCollection<string>(result);
			}

			// Search in current directory
			string[] filesInCurrentDir = Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);

			// Filter files based on exclusion patterns
			foreach (string file in filesInCurrentDir)
			{
				if (!ShouldExcludePath(file, pathExclusionPatterns))
				{
					result.Add(file);
					progressCallback?.Invoke(file);
				}
			}

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

					// Skip excluded directories
					if (ShouldExcludePath(directory, pathExclusionPatterns))
					{
						continue;
					}

					ReadOnlyCollection<string> filesInSubDir = FindFilesWithExclusions(directory, fileName, pathExclusionPatterns, progressCallback);
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
	/// Determines if a path should be excluded based on exclusion patterns
	/// </summary>
	/// <param name="path">The path to check</param>
	/// <param name="exclusionPatterns">The exclusion patterns to match against</param>
	/// <returns>True if the path should be excluded, false otherwise</returns>
	private static bool ShouldExcludePath(string path, IReadOnlyCollection<string> exclusionPatterns)
	{
		if (exclusionPatterns.Count == 0)
		{
			return false;
		}

		string normalizedPath = Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, '/');

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
		// Special handling for common patterns - order matters!
		if (pattern.StartsWith("*/") && pattern.EndsWith("/*"))
		{
			// Pattern like "*/bin/*" - check if path contains the directory
			string dirName = pattern[2..^2]; // Remove "*/" and "/*"
			return path.Contains($"/{dirName}/", StringComparison.OrdinalIgnoreCase);
		}
		else if (pattern.StartsWith('*') && pattern.EndsWith('*') && !pattern.Contains('/'))
		{
			// Pattern like "*node_modules*" - check if any path component contains the substring
			string substring = pattern[1..^1]; // Remove leading and trailing "*"
			string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			return pathComponents.Any(component => component.Contains(substring, StringComparison.OrdinalIgnoreCase));
		}
		else if (pattern.EndsWith('*') && !pattern.Contains('/'))
		{
			// Pattern like "temp*" - check if any path component starts with the prefix
			string prefix = pattern[..^1]; // Remove trailing "*"
			string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			return pathComponents.Any(component => component.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
		}

		// Fallback to general regex pattern matching
		string regexPattern = "^" + pattern
			.Replace("*", ".*")
			.Replace("?", ".")
			.Replace("/", "\\/") + "$";

		try
		{
			// Use a 1-second timeout and non-backtracking algorithm to prevent ReDoS attacks
			return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1));
		}
		catch (ArgumentException)
		{
			// If regex fails, fall back to simple contains check
			return path.Contains(pattern.Replace("*", "").Replace("?", ""), StringComparison.OrdinalIgnoreCase);
		}
		catch (RegexMatchTimeoutException)
		{
			// If regex times out, fall back to simple contains check to avoid DoS
			return path.Contains(pattern.Replace("*", "").Replace("?", ""), StringComparison.OrdinalIgnoreCase);
		}
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
