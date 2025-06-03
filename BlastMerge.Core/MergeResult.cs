// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the result of a merge operation
/// </summary>
/// <param name="MergedLines"> Gets the merged file content as lines </param>
/// <param name="Conflicts"> Gets the conflicts that were encountered during merge </param>
public record MergeResult(IReadOnlyList<string> MergedLines, IReadOnlyCollection<MergeConflict> Conflicts)
{
	/// <summary>
	/// Gets whether all conflicts were successfully resolved
	/// </summary>
	public bool IsFullyResolved => Conflicts.All(c => c.IsResolved);
}

