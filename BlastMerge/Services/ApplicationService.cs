// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;

/// <summary>
/// Implementation of the main application service that handles business logic.
/// </summary>
/// <param name="fileSystemProvider">File system provider for dependency injection</param>
/// <param name="fileFinder">File finder service for dependency injection</param>
/// <param name="fileDiffer">File differ service for dependency injection</param>
public abstract class ApplicationService(IFileSystemProvider fileSystemProvider, FileFinder fileFinder, FileDiffer fileDiffer) : IApplicationService
{
	/// <summary>
	/// The file system provider for dependency injection
	/// </summary>
	private readonly IFileSystemProvider _fileSystemProvider = fileSystemProvider;

	/// <summary>
	/// The file system abstraction to use for file operations (current instance from provider)
	/// </summary>
	protected IFileSystem FileSystem => _fileSystemProvider.Current;

	/// <summary>
	/// The file finder service
	/// </summary>
	protected FileFinder FileFinder { get; } = fileFinder;

	/// <summary>
	/// The file differ service
	/// </summary>
	protected FileDiffer FileDiffer { get; } = fileDiffer;

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public abstract Task ProcessFilesAsync(string directory, string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public abstract Task ProcessBatchAsync(string directory, string batchName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public virtual async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> CompareFilesAsync(string directory, string fileName, CancellationToken cancellationToken = default)
	{
		ValidateDirectoryAndFileName(directory, fileName);

		IReadOnlyCollection<string> filePaths = await Task.Run(() => FileFinder.FindFiles(directory, fileName), cancellationToken).ConfigureAwait(false);
		IReadOnlyCollection<FileGroup> fileGroups = await Task.Run(() => FileDiffer.GroupFilesByHash(filePaths), cancellationToken).ConfigureAwait(false);

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public abstract Task RunIterativeMergeAsync(string directory, string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public abstract Task ListBatchesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts the interactive mode for user input.
	/// This method is UI-specific and should be overridden in the ConsoleApp layer.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public abstract Task StartInteractiveModeAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Synchronous wrapper for ProcessFilesAsync.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public void ProcessFiles(string directory, string fileName) => ProcessFilesAsync(directory, fileName).GetAwaiter().GetResult();

	/// <summary>
	/// Synchronous wrapper for ProcessBatchAsync.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public void ProcessBatch(string directory, string batchName) => ProcessBatchAsync(directory, batchName).GetAwaiter().GetResult();

	/// <summary>
	/// Synchronous wrapper for RunIterativeMergeAsync.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public void RunIterativeMerge(string directory, string fileName) => RunIterativeMergeAsync(directory, fileName).GetAwaiter().GetResult();
}
