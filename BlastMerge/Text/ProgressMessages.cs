// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Text;

/// <summary>
/// Constants for progress messages used throughout the Core library.
/// </summary>
public static class ProgressMessages
{
	/// <summary>
	/// Phase 1 progress message for gathering files.
	/// </summary>
	public const string Phase1GatheringFiles = "🔍 PHASE 1: Gathering files...";

	/// <summary>
	/// Phase 3 progress message for grouping files by content.
	/// </summary>
	public const string Phase3GroupingFiles = "📊 PHASE 3: Grouping files by content...";

	/// <summary>
	/// Phase 4 progress message for resolving conflicts.
	/// </summary>
	public const string Phase4ResolvingConflicts = "🔄 PHASE 4: Resolving conflicts...";
}
