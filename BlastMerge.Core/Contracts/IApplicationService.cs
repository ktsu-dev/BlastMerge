// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Contracts;

using System.Collections.Generic;

/// <summary>
/// Interface for the main application service that handles business logic.
/// </summary>
public interface IApplicationService
{
	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public void ProcessFiles(string directory, string fileName);

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public void ProcessBatch(string directory, string batchName);

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName);

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public void RunIterativeMerge(string directory, string fileName);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public void ListBatches();

	/// <summary>
	/// Starts the interactive mode for user input.
	/// </summary>
	public void StartInteractiveMode();
}
