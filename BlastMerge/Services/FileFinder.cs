// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName, IFileSystem? fileSystem = null) =>
		FindFiles(rootDirectory, fileName, fileSystem, null);

	/// <summary>
	/// Recursively finds all files with the specified filename with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to FileSystemProvider.Current)</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(string rootDirectory, string fileName, IFileSystem? fileSystem = null, Action<string>? progressCallback = null)
	{
		fileSystem ??= FileSystemProvider.Current;
		List<string> result = [];

		try
		{
			// Search in current directory
			string[] filesInCurrentDir = fileSystem.Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);
			foreach (string file in filesInCurrentDir)
			{
				result.Add(file);
				progressCallback?.Invoke(file);
			}

			// Search in subdirectories
			foreach (string directory in fileSystem.Directory.GetDirectories(rootDirectory).Where(dir => !IsGitSubmodule(dir, fileSystem)))
			{
				try
				{
					IReadOnlyCollection<string> filesInSubDir = FindFiles(directory, fileName, fileSystem, progressCallback);
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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		IFileSystem? fileSystem = null) =>
		FindFiles(searchPaths, rootDirectory, fileName, pathExclusionPatterns, fileSystem, null);

	/// <summary>
	/// Finds all files matching the specified filename across multiple search paths with exclusion support and progress reporting
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to FileSystemProvider.Current)</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public static IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		IFileSystem? fileSystem = null,
		Action<string>? progressCallback = null)
	{
		ArgumentNullException.ThrowIfNull(searchPaths);
		ArgumentNullException.ThrowIfNull(rootDirectory);
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(pathExclusionPatterns);

		fileSystem ??= FileSystemProvider.Current;
		List<string> result = [];

		// Use search paths if provided, otherwise use the root directory
		IEnumerable<string> directoriesToSearch = searchPaths.Count > 0 ? searchPaths : [rootDirectory];

		result.AddRange(directoriesToSearch
			.Where(fileSystem.Directory.Exists)
			.SelectMany(searchPath => FindFilesWithExclusions(searchPath, fileName, pathExclusionPatterns, fileSystem, progressCallback)));

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
		FindFilesWithExclusions(rootDirectory, fileName, pathExclusionPatterns, FileSystemProvider.Current, progressCallback);

	/// <summary>
	/// Recursively finds all files with the specified filename, applying exclusion patterns with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="fileSystem">File system abstraction</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	private static ReadOnlyCollection<string> FindFilesWithExclusions(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		IFileSystem fileSystem,
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

			ProcessCurrentDirectory(rootDirectory, fileName, pathExclusionPatterns, fileSystem, progressCallback, result);
			ProcessSubdirectories(rootDirectory, fileName, pathExclusionPatterns, fileSystem, progressCallback, result);
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
		{
			// Log or handle exception as needed
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Processes files in the current directory
	/// </summary>
	private static void ProcessCurrentDirectory(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		IFileSystem fileSystem,
		Action<string>? progressCallback,
		List<string> result)
	{
		string[] filesInCurrentDir = fileSystem.Directory.GetFiles(rootDirectory, fileName, SearchOption.TopDirectoryOnly);

		foreach (string file in filesInCurrentDir.Where(file => !ShouldExcludePath(file, pathExclusionPatterns)))
		{
			result.Add(file);
			progressCallback?.Invoke(file);
		}
	}

	/// <summary>
	/// Processes subdirectories recursively
	/// </summary>
	private static void ProcessSubdirectories(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		IFileSystem fileSystem,
		Action<string>? progressCallback,
		List<string> result)
	{
		foreach (string directory in fileSystem.Directory.GetDirectories(rootDirectory).Where(dir => !ShouldSkipDirectory(dir, pathExclusionPatterns, fileSystem)))
		{
			try
			{
				ReadOnlyCollection<string> filesInSubDir = FindFilesWithExclusions(directory, fileName, pathExclusionPatterns, fileSystem, progressCallback);
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
	/// Determines if a directory should be skipped
	/// </summary>
	private static bool ShouldSkipDirectory(string directory, IReadOnlyCollection<string> pathExclusionPatterns, IFileSystem fileSystem) =>
		IsGitSubmodule(directory, fileSystem) || ShouldExcludePath(directory, pathExclusionPatterns);

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

		return exclusionPatterns
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Any(pattern =>
			{
				// Support simple glob patterns
				string normalizedPattern = pattern.Replace('\\', '/');

				// Convert simple glob patterns to regex-like matching
				if (normalizedPattern.Contains('*') || normalizedPattern.Contains('?'))
				{
					return MatchesGlobPattern(normalizedPath, normalizedPattern);
				}
				else
				{
					// Exact match or contains check
					return normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase);
				}
			});
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
		if (IsDirectoryContainsPattern(pattern))
		{
			return MatchesDirectoryContainsPattern(path, pattern);
		}

		if (IsWildcardContainsPattern(pattern))
		{
			return MatchesWildcardContainsPattern(path, pattern);
		}

		if (IsPrefixPattern(pattern))
		{
			return MatchesPrefixPattern(path, pattern);
		}

		// Fallback to general regex pattern matching
		return MatchesRegexPattern(path, pattern);
	}

	/// <summary>
	/// Checks if pattern is a directory contains pattern like "*/bin/*"
	/// </summary>
	private static bool IsDirectoryContainsPattern(string pattern) =>
		pattern.StartsWith("*/") && pattern.EndsWith("/*");

	/// <summary>
	/// Matches directory contains patterns like "*/bin/*"
	/// </summary>
	private static bool MatchesDirectoryContainsPattern(string path, string pattern)
	{
		string dirName = pattern[2..^2]; // Remove "*/" and "/*"
		return path.Contains($"/{dirName}/", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Checks if pattern is a wildcard contains pattern like "*node_modules*"
	/// </summary>
	private static bool IsWildcardContainsPattern(string pattern) =>
		pattern.StartsWith('*') && pattern.EndsWith('*') && !pattern.Contains('/');

	/// <summary>
	/// Matches wildcard contains patterns like "*node_modules*"
	/// </summary>
	private static bool MatchesWildcardContainsPattern(string path, string pattern)
	{
		string substring = pattern[1..^1]; // Remove leading and trailing "*"
		string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return pathComponents.Any(component => component.Contains(substring, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Checks if pattern is a prefix pattern like "temp*"
	/// </summary>
	private static bool IsPrefixPattern(string pattern) =>
		pattern.EndsWith('*') && !pattern.Contains('/');

	/// <summary>
	/// Matches prefix patterns like "temp*"
	/// </summary>
	private static bool MatchesPrefixPattern(string path, string pattern)
	{
		string prefix = pattern[..^1]; // Remove trailing "*"
		string[] pathComponents = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return pathComponents.Any(component => component.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Matches using regex pattern as fallback
	/// </summary>
	private static bool MatchesRegexPattern(string path, string pattern)
	{
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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>True if the directory is a git submodule, false otherwise</returns>
	private static bool IsGitSubmodule(string directoryPath, IFileSystem? fileSystem = null)
	{
		fileSystem ??= new FileSystem();
		try
		{
			string gitPath = fileSystem.Path.Combine(directoryPath, ".git");

			// Git submodules have a .git file (not directory) that contains a reference
			// to the actual git directory location
			return fileSystem.File.Exists(gitPath) && !fileSystem.Directory.Exists(gitPath);
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
