// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

using System.Collections.Generic;

/// <summary>
/// Interface for handling interactive merge UI operations.
/// </summary>
public interface IInteractiveMergeService
{
	/// <summary>
	/// Performs iterative merge on file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to merge.</param>
	public void PerformIterativeMerge(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups);
}
