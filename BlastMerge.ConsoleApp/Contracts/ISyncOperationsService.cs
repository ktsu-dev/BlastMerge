// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

using System.Collections.Generic;

/// <summary>
/// Interface for file synchronization UI operations service.
/// </summary>
public interface ISyncOperationsService
{
	/// <summary>
	/// Offers sync options for file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to offer sync options for.</param>
	public void OfferSyncOptions(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups);
}
