// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Main application data storage for BlastMerge, managing all persistent state.
/// </summary>
public class AppData
{
	/// <summary>
	/// Gets or sets the collection of batch configurations.
	/// </summary>
	public Dictionary<string, BatchConfiguration> BatchConfigurations { get; init; } = [];

	/// <summary>
	/// Gets or sets the input history organized by prompt type.
	/// </summary>
	public Dictionary<string, Collection<string>> InputHistory { get; init; } = [];
}
