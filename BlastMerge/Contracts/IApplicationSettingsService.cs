// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Service for managing application settings with persistent storage.
/// </summary>
public interface IApplicationSettingsService
{
	/// <summary>
	/// Gets the current application settings.
	/// </summary>
	/// <returns>The current application settings.</returns>
	public Task<ApplicationSettings> GetSettingsAsync();

	/// <summary>
	/// Saves the application settings.
	/// </summary>
	/// <param name="settings">The settings to save.</param>
	public Task SaveSettingsAsync(ApplicationSettings settings);

	/// <summary>
	/// Resets settings to default values.
	/// </summary>
	public Task ResetToDefaultsAsync();
}
