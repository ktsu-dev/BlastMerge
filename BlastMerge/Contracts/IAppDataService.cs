// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Service for managing the application data.
/// </summary>
public interface IAppDataService
{
	/// <summary>
	/// Gets the application data.
	/// </summary>
	public AppData AppData { get; }

	/// <summary>
	/// Saves the application data.
	/// </summary>
	public Task SaveAsync();
}
