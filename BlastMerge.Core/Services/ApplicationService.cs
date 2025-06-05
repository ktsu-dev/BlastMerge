// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Core.Contracts;
using ktsu.BlastMerge.Core.Models;

/// <summary>
/// Implementation of the main application service that handles business logic.
/// </summary>
public class ApplicationService : IApplicationService
{
	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public virtual void ProcessFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);
		// Process files as needed
	}

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public virtual void ProcessBatch(string directory, string batchName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(batchName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Process batch configuration
	}

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public virtual IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);
		return (IReadOnlyDictionary<string, IReadOnlyCollection<string>>)FileDiffer.GroupFilesByHash(filePaths);
	}

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public virtual void RunIterativeMerge(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null)
		{
			return; // No files found or insufficient unique versions to merge
		}

		// Start iterative merge process with default callbacks
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			DefaultMergeCallback,
			DefaultStatusCallback,
			DefaultContinueCallback);
	}

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public virtual void ListBatches()
	{
		IReadOnlyCollection<string> batchNames = BatchManager.ListBatches();
		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

		// Core implementation - no UI dependencies
		// The data is available, UI layer will handle display
	}

	/// <summary>
	/// Starts the interactive mode for user input.
	/// This method is UI-specific and should be overridden in the ConsoleApp layer.
	/// </summary>
	public virtual void StartInteractiveMode()
	{
		// Core implementation - no UI dependencies
		// ConsoleApp will override this with UI-specific implementation
	}

	/// <summary>
	/// Default callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private static MergeResult? DefaultMergeCallback(string file1, string file2, string? existingContent)
	{
		// Default automatic merge - use version 1
		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			existingContent,
			(block, context, index) => BlockChoice.UseVersion1);
	}

	/// <summary>
	/// Default callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private static void DefaultStatusCallback(MergeSessionStatus status)
	{
		// No output in core - UI layer will handle this
	}

	/// <summary>
	/// Default callback to determine if merge should continue.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private static bool DefaultContinueCallback() =>
		// Default to continue
		true;
}
