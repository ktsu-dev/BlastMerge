// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

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
		Ensure.NotNull(searchPaths);
		Ensure.NotNull(rootDirectory);
		Ensure.NotNull(fileName);
		Ensure.NotNull(pathExclusionPatterns);

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
	private static bool ShouldExcludePath(string path, IReadOnlyCollection<string> exclusionPatterns) =>
		PathExclusionMatcher.IsExcluded(
			Path.GetFullPath(path).Replace(Path.DirectorySeparatorChar, '/'),
			exclusionPatterns);

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
