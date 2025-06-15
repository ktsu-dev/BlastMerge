// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;

/// <summary>
/// Implementation of the main application service that handles business logic.
/// </summary>
/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
public abstract class ApplicationService(IFileSystem? fileSystem = null) : IApplicationService
{
	/// <summary>
	/// The file system abstraction to use for file operations
	/// </summary>
	protected IFileSystem FileSystem { get; } = fileSystem ?? new FileSystem();

	/// <summary>
	/// Validates that parameters are not null and directory exists.
	/// </summary>
	/// <param name="directory">The directory to validate.</param>
	/// <param name="fileName">The filename pattern to validate.</param>
	/// <exception cref="ArgumentNullException">Thrown when directory or fileName is null.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist.</exception>
	protected void ValidateDirectoryAndFileName(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);
		ValidateDirectoryExists(directory);
	}

	/// <summary>
	/// Validates that the directory exists.
	/// </summary>
	/// <param name="directory">The directory to validate.</param>
	/// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist.</exception>
	protected void ValidateDirectoryExists(string directory)
	{
		if (!FileSystem.Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}
	}

	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public abstract void ProcessFiles(string directory, string fileName);

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public abstract void ProcessBatch(string directory, string batchName);

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public virtual IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName)
	{
		ValidateDirectoryAndFileName(directory, fileName);

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName, FileSystem);
		IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(filePaths, FileSystem);

		// Convert FileGroup collection to Dictionary<string, IReadOnlyCollection<string>>
		// Use a combination of hash and index to ensure unique keys
		Dictionary<string, IReadOnlyCollection<string>> result = [];
		int index = 0;
		foreach (FileGroup group in fileGroups)
		{
			string key = $"{group.Hash}_{index++}";
			result[key] = group.FilePaths;
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public abstract void RunIterativeMerge(string directory, string fileName);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public abstract void ListBatches();

	/// <summary>
	/// Starts the interactive mode for user input.
	/// This method is UI-specific and should be overridden in the ConsoleApp layer.
	/// </summary>
	public abstract void StartInteractiveMode();
}
