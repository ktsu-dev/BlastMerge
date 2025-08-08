// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Text;

/// <summary>
/// Constants for progress messages used throughout the Core library.
/// </summary>
public static class ProgressMessages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public const string Phase1GatheringFiles = "🔍 PHASE 1: Gathering files...";
	public const string Phase3GroupingFiles = "📊 PHASE 3: Grouping files by content...";
	public const string Phase4ResolvingConflicts = "🔄 PHASE 4: Resolving conflicts...";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
