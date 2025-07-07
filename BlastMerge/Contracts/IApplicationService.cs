// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task ProcessFilesAsync(string directory, string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task ProcessBatchAsync(string directory, string batchName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> CompareFilesAsync(string directory, string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task RunIterativeMergeAsync(string directory, string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task ListBatchesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts the interactive mode for user input.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task StartInteractiveModeAsync(CancellationToken cancellationToken = default);
}
