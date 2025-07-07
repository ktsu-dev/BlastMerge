// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using ktsu.FileSystemProvider;

/// <summary>
/// Provides file finding capabilities with pattern matching support.
/// </summary>
/// <param name="fileSystemProvider">File system provider for dependency injection</param>
public class FileFinder(IFileSystemProvider fileSystemProvider)
{
	private readonly IFileSystemProvider _fileSystemProvider = fileSystemProvider;
	/// <summary>
	/// Finds files matching the specified patterns in the given directory.
	/// </summary>
	/// <param name="directory">The directory to search in.</param>
	/// <param name="patterns">The patterns to match against file names.</param>
	/// <returns>A collection of file paths that match the patterns.</returns>
	public IEnumerable<string> FindFiles(string directory, IEnumerable<string> patterns)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);
		ArgumentNullException.ThrowIfNull(patterns);

		IFileSystem fileSystem = _fileSystemProvider.Current; // Cache once for this method

		if (!fileSystem.Directory.Exists(directory))
		{
			yield break;
		}

		List<string> patternList = [.. patterns];
		if (patternList.Count == 0)
		{
			yield break;
		}

		foreach (string filePath in GetAllFiles(directory))
		{
			string fileName = fileSystem.Path.GetFileName(filePath);
			if (patternList.Any(pattern => MatchesPattern(fileName, pattern)))
			{
				yield return filePath;
			}
		}
	}

	/// <summary>
	/// Finds files matching a single pattern in the given directory.
	/// </summary>
	/// <param name="directory">The directory to search in.</param>
	/// <param name="pattern">The pattern to match against file names.</param>
	/// <returns>A collection of file paths that match the pattern.</returns>
	public IReadOnlyCollection<string> FindFiles(string directory, string pattern) => FindFiles(directory, [pattern]).ToList().AsReadOnly();

	/// <summary>
	/// Recursively gets all files in the specified directory and its subdirectories.
	/// </summary>
	/// <param name="directory">The directory to search.</param>
	/// <returns>An enumerable of file paths.</returns>
	private IEnumerable<string> GetAllFiles(string directory)
	{
		IFileSystem fileSystem = _fileSystemProvider.Current; // Cache once for this method

		// Get files in current directory
		foreach (string file in fileSystem.Directory.GetFiles(directory))
		{
			yield return file;
		}

		// Recursively get files from subdirectories
		foreach (string subDirectory in fileSystem.Directory.GetDirectories(directory))
		{
			foreach (string file in GetAllFiles(subDirectory))
			{
				yield return file;
			}
		}
	}

	/// <summary>
	/// Determines if a file name matches the specified pattern.
	/// </summary>
	/// <param name="fileName">The file name to check.</param>
	/// <param name="pattern">The pattern to match against.</param>
	/// <returns>True if the file name matches the pattern; otherwise, false.</returns>
	private static bool MatchesPattern(string fileName, string pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern))
		{
			return false;
		}

		// Handle exact matches
		if (!pattern.Contains('*', StringComparison.Ordinal) && !pattern.Contains('?', StringComparison.Ordinal))
		{
			return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);
		}

		// Convert wildcard pattern to regex
		string regexPattern = ConvertWildcardToRegex(pattern);
		return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Converts a wildcard pattern to a regular expression pattern.
	/// </summary>
	/// <param name="wildcardPattern">The wildcard pattern to convert.</param>
	/// <returns>A regular expression pattern.</returns>
	private static string ConvertWildcardToRegex(string wildcardPattern)
	{
		string pattern = Regex.Escape(wildcardPattern);
		pattern = pattern.Replace("\\*", ".*", StringComparison.Ordinal);
		pattern = pattern.Replace("\\?", ".", StringComparison.Ordinal);
		return $"^{pattern}$";
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
		IReadOnlyCollection<string> pathExclusionPatterns) =>
		FindFiles(searchPaths, rootDirectory, fileName, pathExclusionPatterns, null);

	/// <summary>
	/// Static method: Finds all files matching the specified filename across multiple search paths with exclusion support and progress reporting
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="fileSystem">File system provider (optional, defaults to FileSystemProvider.Current)</param>
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
	/// Finds all files matching the specified filename across multiple search paths with exclusion support and progress reporting
	/// </summary>
	/// <param name="searchPaths">The search paths (directories) to search from. If empty, uses rootDirectory.</param>
	/// <param name="rootDirectory">The fallback root directory to search from if searchPaths is empty</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	public IReadOnlyCollection<string> FindFiles(
		IReadOnlyCollection<string> searchPaths,
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Action<string>? progressCallback = null)
	{
		ArgumentNullException.ThrowIfNull(searchPaths);
		ArgumentNullException.ThrowIfNull(rootDirectory);
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(pathExclusionPatterns);

		List<string> result = [];

		// Use search paths if provided, otherwise use the root directory
		IEnumerable<string> directoriesToSearch = searchPaths.Count > 0 ? searchPaths : [rootDirectory];

		IFileSystem fileSystem = _fileSystemProvider.Current; // Cache once for this method

		result.AddRange(directoriesToSearch
			.Where(fileSystem.Directory.Exists)
			.SelectMany(searchPath => FindFilesWithExclusions(searchPath, fileName, pathExclusionPatterns, progressCallback)));

		return result.AsReadOnly();
	}

	/// <summary>
	/// Static method: Recursively finds all files with the specified filename, applying exclusion patterns with progress reporting
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
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <returns>A list of full file paths</returns>
	private ReadOnlyCollection<string> FindFilesWithExclusions(
		string rootDirectory,
		string fileName,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Action<string>? progressCallback)
	{
		IFileSystem fileSystem = _fileSystemProvider.Current; // Cache once for this method
		return FindFilesWithExclusions(rootDirectory, fileName, pathExclusionPatterns, fileSystem, progressCallback);
	}

	/// <summary>
	/// Static version: Recursively finds all files with the specified filename, applying exclusion patterns with progress reporting
	/// </summary>
	/// <param name="rootDirectory">The root directory to search from</param>
	/// <param name="fileName">The filename to search for</param>
	/// <param name="pathExclusionPatterns">Path patterns to exclude from the search</param>
	/// <param name="fileSystem">File system provider</param>
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

		// Normalize the path for consistent comparison
		string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');

		foreach (string pattern in exclusionPatterns)
		{
			if (MatchesGlobPattern(normalizedPath, pattern))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a path matches a glob pattern
	/// </summary>
	/// <param name="path">The path to check</param>
	/// <param name="pattern">The glob pattern</param>
	/// <returns>True if the path matches the pattern, false otherwise</returns>
	private static bool MatchesGlobPattern(string path, string pattern)
	{
		// Handle different types of patterns
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

		// Fall back to regex pattern matching
		return MatchesRegexPattern(path, pattern);
	}

	/// <summary>
	/// Checks if a pattern is a directory contains pattern (e.g., */bin/*)
	/// </summary>
	private static bool IsDirectoryContainsPattern(string pattern) =>
		pattern.Contains('/') && pattern.Contains('*');

	/// <summary>
	/// Matches directory contains patterns like */bin/*
	/// </summary>
	private static bool MatchesDirectoryContainsPattern(string path, string pattern)
	{
		string dirName = pattern.Trim('*', '/');
		return path.Split('/').Contains(dirName, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Checks if a pattern is a wildcard contains pattern (e.g., *node_modules*)
	/// </summary>
	private static bool IsWildcardContainsPattern(string pattern) =>
		pattern.StartsWith('*') && pattern.EndsWith('*') && !pattern.Contains('/');

	/// <summary>
	/// Matches wildcard contains patterns like *node_modules*
	/// </summary>
	private static bool MatchesWildcardContainsPattern(string path, string pattern)
	{
		string substring = pattern.Trim('*');
		return path.Contains(substring, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Checks if a pattern is a prefix pattern (e.g., temp*)
	/// </summary>
	private static bool IsPrefixPattern(string pattern) =>
		pattern.EndsWith('*') && !pattern.StartsWith('*') && !pattern.Contains('/');

	/// <summary>
	/// Matches prefix patterns like temp*
	/// </summary>
	private static bool MatchesPrefixPattern(string path, string pattern)
	{
		string prefix = pattern.TrimEnd('*');
		return Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Matches patterns using regex with timeout protection
	/// </summary>
	private static bool MatchesRegexPattern(string path, string pattern)
	{
		try
		{
			// Convert glob pattern to regex
			string regexPattern = "^" + Regex.Escape(pattern)
				.Replace("\\*", ".*")
				.Replace("\\?", ".") + "$";

			// Use timeout to prevent ReDoS attacks
			return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
		}
		catch (RegexMatchTimeoutException)
		{
			// If regex times out, fall back to simple contains check
			return path.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase);
		}
		catch (ArgumentException)
		{
			// If regex is invalid, fall back to simple contains check
			return path.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// Checks if a directory is a Git submodule
	/// </summary>
	/// <param name="directoryPath">The directory path to check</param>
	/// <param name="fileSystem">File system provider</param>
	/// <returns>True if the directory is a Git submodule, false otherwise</returns>
	private static bool IsGitSubmodule(string directoryPath, IFileSystem fileSystem)
	{
		try
		{
			string gitModulesPath = fileSystem.Path.Combine(directoryPath, ".git");
			if (fileSystem.File.Exists(gitModulesPath))
			{
				string content = fileSystem.File.ReadAllText(gitModulesPath);
				return content.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase);
			}
		}
		catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
		{
			// If we can't read the .git file, assume it's not a submodule
		}

		return false;
	}
}
