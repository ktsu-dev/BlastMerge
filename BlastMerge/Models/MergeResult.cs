// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Services;

/// <summary>
/// Represents the result of a merge operation
/// </summary>
/// <param name="MergedLines"> Gets the merged file content as lines </param>
/// <param name="Conflicts"> Gets the conflicts that were encountered during merge </param>
/// <param name="LineEnding"> Gets the line ending the merged content should be written with </param>
/// <remarks>
/// <paramref name="LineEnding"/> is detected from the merged sources rather than resolved at each
/// write site. Reassembling the lines with <see cref="Environment.NewLine"/> instead would make the
/// style of the written file depend on the host OS, and rewrite every line of a CRLF file merged on
/// Linux (or an LF file merged on Windows) into the other style.
/// </remarks>
public record MergeResult(IReadOnlyList<string> MergedLines, IReadOnlyCollection<MergeConflict> Conflicts, string LineEnding)
{
	/// <summary>
	/// Initializes a merge result whose sources carried no detectable line-ending style.
	/// </summary>
	/// <param name="mergedLines">The merged file content as lines.</param>
	/// <param name="conflicts">The conflicts that were encountered during merge.</param>
	public MergeResult(IReadOnlyList<string> mergedLines, IReadOnlyCollection<MergeConflict> conflicts)
		: this(mergedLines, conflicts, Environment.NewLine)
	{
	}

	/// <summary>
	/// Gets whether all conflicts were successfully resolved
	/// </summary>
	public bool IsFullyResolved => Conflicts.All(c => c.IsResolved);

	/// <summary>
	/// Renders the merged lines as file content using the detected line ending.
	/// </summary>
	/// <returns>The merged content ready to be written to disk.</returns>
	public string ToContent() => LineEndingDetector.Join(MergedLines, LineEnding);
}

