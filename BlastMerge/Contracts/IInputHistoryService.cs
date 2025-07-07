// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Service for managing input history with persistent storage.
/// </summary>
public interface IInputHistoryService
{
	/// <summary>
	/// Adds an entry to the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="value">The value to add.</param>
	public Task AddToHistoryAsync(PromptKey promptKey, string value);

	/// <summary>
	/// Gets the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <returns>The history list for the prompt.</returns>
	public Task<IReadOnlyList<string>> GetHistoryForPromptAsync(PromptKey promptKey);

	/// <summary>
	/// Gets the count of history entries for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <returns>The number of history entries.</returns>
	public Task<int> GetHistoryCountAsync(PromptKey promptKey);

	/// <summary>
	/// Gets all history entries for debugging or inspection.
	/// </summary>
	/// <returns>A read-only dictionary of all history entries.</returns>
	public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllHistoryAsync();

	/// <summary>
	/// Clears all input history.
	/// </summary>
	public Task ClearAllHistoryAsync();
}
