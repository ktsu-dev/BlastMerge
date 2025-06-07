// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Text;

/// <summary>
/// Diff format choice display texts.
/// </summary>
public static class DiffFormatsDisplay
{
	/// <summary>
	/// Change Summary diff format display text.
	/// </summary>
	public const string ChangeSummary = "📊 Change Summary (Added/Removed lines only)";

	/// <summary>
	/// Git-style diff format display text.
	/// </summary>
	public const string GitStyleDiff = "🔧 Git-style Diff (Full context)";

	/// <summary>
	/// Side-by-Side diff format display text.
	/// </summary>
	public const string SideBySideDiff = "🎨 Side-by-Side Diff (Rich formatting)";

	/// <summary>
	/// Skip comparison display text.
	/// </summary>
	public const string SkipComparison = "⏭️ Skip comparison";
}
