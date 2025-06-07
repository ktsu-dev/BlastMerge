// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Constants for menu names used throughout the application for navigation and display.
/// </summary>
public static class MenuNames
{
	/// <summary>
	/// Main menu identifier.
	/// </summary>
	public const string MainMenu = "Main Menu";

	/// <summary>
	/// Find Files menu identifier.
	/// </summary>
	public const string FindFiles = "Find Files";

	/// <summary>
	/// Compare Files menu identifier.
	/// </summary>
	public const string CompareFiles = "Compare Files";

	/// <summary>
	/// Iterative Merge menu identifier.
	/// </summary>
	public const string IterativeMerge = "Iterative Merge";

	/// <summary>
	/// Batch Operations menu identifier.
	/// </summary>
	public const string BatchOperations = "Batch Operations";

	/// <summary>
	/// Settings menu identifier.
	/// </summary>
	public const string Settings = "Settings";

	/// <summary>
	/// Help menu identifier.
	/// </summary>
	public const string Help = "Help";

	// Menu display texts with emojis
	/// <summary>
	/// Main menu items with emojis for display.
	/// </summary>
	public static class Display
	{
		/// <summary>
		/// Find Files menu display text.
		/// </summary>
		public const string FindFiles = "🔍 Find Files";

		/// <summary>
		/// Compare Files menu display text.
		/// </summary>
		public const string CompareFiles = "📊 Compare Files";

		/// <summary>
		/// Iterative Merge menu display text.
		/// </summary>
		public const string IterativeMerge = "🔀 Iterative Merge";

		/// <summary>
		/// Batch Operations menu display text.
		/// </summary>
		public const string BatchOperations = "📦 Batch Operations";

		/// <summary>
		/// Run Recent Batch menu display text.
		/// </summary>
		public const string RunRecentBatch = "🚀 Run Recent Batch";

		/// <summary>
		/// Configuration and Settings menu display text.
		/// </summary>
		public const string Settings = "⚙  Configuration & Settings";

		/// <summary>
		/// Help and Information menu display text.
		/// </summary>
		public const string Help = "❓ Help & Information";

		/// <summary>
		/// Exit menu display text.
		/// </summary>
		public const string Exit = "🚪 Exit";
	}

	/// <summary>
	/// Batch operations submenu display texts.
	/// </summary>
	public static class BatchOperationsDisplay
	{
		/// <summary>
		/// Run Batch Configuration display text.
		/// </summary>
		public const string RunBatchConfiguration = "▶️ Run Batch Configuration";

		/// <summary>
		/// Manage Batch Configurations display text.
		/// </summary>
		public const string ManageBatchConfigurations = "⚙️ Manage Batch Configurations";
	}

	/// <summary>
	/// Compare files submenu display texts.
	/// </summary>
	public static class CompareFilesDisplay
	{
		/// <summary>
		/// Compare Files in Directory display text.
		/// </summary>
		public const string CompareFilesInDirectory = "🔍 Compare Files in Directory";

		/// <summary>
		/// Compare Two Directories display text.
		/// </summary>
		public const string CompareTwoDirectories = "📁 Compare Two Directories";

		/// <summary>
		/// Compare Two Specific Files display text.
		/// </summary>
		public const string CompareTwoSpecificFiles = "📄 Compare Two Specific Files";
	}

	/// <summary>
	/// Settings submenu display texts.
	/// </summary>
	public static class SettingsDisplay
	{
		/// <summary>
		/// View Configuration Paths display text.
		/// </summary>
		public const string ViewConfigurationPaths = "📁 View Configuration Paths";

		/// <summary>
		/// Clear Input History display text.
		/// </summary>
		public const string ClearInputHistory = "🧹 Clear Input History";

		/// <summary>
		/// View Statistics display text.
		/// </summary>
		public const string ViewStatistics = "📊 View Statistics";
	}

	// Action choice display texts
	/// <summary>
	/// Action choice display texts.
	/// </summary>
	public static class Actions
	{
		/// <summary>
		/// Show differences between versions display text.
		/// </summary>
		public const string ShowDifferences = "🔍 Show differences between versions";

		/// <summary>
		/// Run iterative merge on duplicates display text.
		/// </summary>
		public const string RunIterativeMergeOnDuplicates = "🔄 Run iterative merge on duplicates";

		/// <summary>
		/// Use both files display text.
		/// </summary>
		public const string UseBoth = "🔄 Use Both";

		/// <summary>
		/// View detailed file list display text.
		/// </summary>
		public const string ViewDetailedFileList = "📋 View detailed file list";

		/// <summary>
		/// Sync files to make them identical display text.
		/// </summary>
		public const string SyncFiles = "🔁 Sync files to make them identical";

		/// <summary>
		/// Return to main menu display text.
		/// </summary>
		public const string ReturnToMainMenu = "🏠 Return to main menu";

		/// <summary>
		/// Skip this pattern display text.
		/// </summary>
		public const string SkipPattern = "⏭️ Skip this pattern";

		/// <summary>
		/// Stop processing display text.
		/// </summary>
		public const string StopProcessing = "🛑 Stop processing";
	}

	/// <summary>
	/// Sync operation display texts.
	/// </summary>
	public static class SyncOperations
	{
		/// <summary>
		/// Sync all files to newest version display text.
		/// </summary>
		public const string SyncToNewest = "🔄 Sync all files to newest version";

		/// <summary>
		/// Choose a reference file for each group display text.
		/// </summary>
		public const string ChooseReference = "📂 Choose a reference file for each group";

		/// <summary>
		/// Back to previous menu display text.
		/// </summary>
		public const string BackToPreviousMenu = "🔙 Back to previous menu";
	}

	/// <summary>
	/// Output and status message display texts.
	/// </summary>
	public static class Output
	{
		/// <summary>
		/// Batch Processing Summary header text.
		/// </summary>
		public const string BatchProcessingSummary = "📊 Batch Processing Summary";

		/// <summary>
		/// Detailed Merge Operations header text.
		/// </summary>
		public const string DetailedMergeOperations = "🔄 Detailed Merge Operations";

		/// <summary>
		/// Merge Operations Summary header text.
		/// </summary>
		public const string MergeOperationsSummary = "🔄 Merge Operations Summary";

		/// <summary>
		/// Block identifier prefix for merge operations.
		/// </summary>
		public const string BlockPrefix = "🔍 Block";
	}

	/// <summary>
	/// Diff format choice display texts.
	/// </summary>
	public static class DiffFormats
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
}
