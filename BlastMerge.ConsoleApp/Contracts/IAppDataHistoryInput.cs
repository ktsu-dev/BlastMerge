// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for application data history input service.
/// </summary>
public interface IAppDataHistoryInput
{
	/// <summary>
	/// Asks for input with history support.
	/// </summary>
	/// <param name="prompt">The prompt to display.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The user input.</returns>
	public Task<string> AskWithHistoryAsync(string prompt, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all history entries for debugging or inspection.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A read-only dictionary of all history entries.</returns>
	public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllHistoryAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Clears all history entries.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task representing the asynchronous operation.</returns>
	public Task ClearAllHistoryAsync(CancellationToken cancellationToken = default);
}
