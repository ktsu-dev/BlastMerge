// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Linq;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="ProgressReportingService"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ProgressReportingServiceTests : ConsoleTestBase
{
	private const char DeletionMark = '−';

	/// <summary>
	/// Gets the line of output containing <paramref name="text"/>.
	/// </summary>
	/// <param name="text">Text the line contains.</param>
	/// <returns>The first matching line.</returns>
	private string LineContaining(string text) =>
		Output.Split('\n').First(line => line.Contains(text, StringComparison.Ordinal));

	/// <summary>
	/// Gets the tug-of-war bar that follows the colon on the merge header line.
	/// </summary>
	/// <returns>The bar text.</returns>
	private string TugOfWarBar()
	{
		string header = LineContaining("Merging");
		return header[(header.IndexOf("):", StringComparison.Ordinal) + 2)..].Trim();
	}

	/// <summary>
	/// The status report names the pair being merged, its similarity and the files left.
	/// </summary>
	[TestMethod]
	public void ReportMergeStatus_WithPair_ShowsPairSimilarityAndRemaining()
	{
		MergeSessionStatus status = new(2, 4, 1, new FileSimilarity("one.txt", "two.txt", 87.5));

		ProgressReportingService.ReportMergeStatus(status);

		StringAssert.Contains(Output, "Merge 2: one.txt <-> two.txt");
		StringAssert.Contains(Output, $"Similarity: {87.5:F1} | Remaining files: 4");
	}

	/// <summary>
	/// The status report tolerates a session with no pair selected yet.
	/// </summary>
	[TestMethod]
	public void ReportMergeStatus_WithoutPair_ShowsBlankPair()
	{
		ProgressReportingService.ReportMergeStatus(new MergeSessionStatus(1, 3, 0, null));

		StringAssert.Contains(Output, "Merge 1:  <-> ");
		StringAssert.Contains(Output, "Remaining files: 3");
	}

	/// <summary>
	/// Paths containing markup characters are printed literally.
	/// </summary>
	[TestMethod]
	public void ReportMergeStatus_PathWithBrackets_IsPrintedLiterally()
	{
		ProgressReportingService.ReportMergeStatus(new MergeSessionStatus(1, 2, 0, new FileSimilarity("[a].txt", "[b].txt", 50)));

		StringAssert.Contains(Output, "[a].txt <-> [b].txt");
	}

	/// <summary>
	/// A successful completion names the final file.
	/// </summary>
	[TestMethod]
	public void ReportCompletionResult_Success_ReportsFinalFile()
	{
		ProgressReportingService.ReportCompletionResult(new MergeCompletionResult(true, "x", 1, "final.txt"));

		StringAssert.Contains(Output, "Merge completed successfully. Final file: final.txt");
	}

	/// <summary>
	/// A failed completion is reported as failed or cancelled.
	/// </summary>
	[TestMethod]
	public void ReportCompletionResult_Failure_ReportsFailure()
	{
		ProgressReportingService.ReportCompletionResult(new MergeCompletionResult(false, null, 0, "final.txt"));

		StringAssert.Contains(Output, "Merge failed or was cancelled: final.txt");
		Assert.DoesNotContain("completed successfully", Output);
	}

	/// <summary>
	/// The reporting methods that take a model reject null.
	/// </summary>
	[TestMethod]
	public void ModelMethods_Null_Throw()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ProgressReportingService.ReportMergeStatus(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => ProgressReportingService.ReportCompletionResult(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => ProgressReportingService.ShowDetailedMergeSummary(null!));
	}

	/// <summary>
	/// A successful summary tabulates the overall metrics and each operation with short labels.
	/// </summary>
	[TestMethod]
	public void ShowDetailedMergeSummary_SuccessfulWithOperations_ShowsMetricsAndOperations()
	{
		MergeCompletionResult result = new(true, "merged", 42, "file.txt")
		{
			InitialFileGroups = 3,
			TotalFilesMerged = 6,
			TotalMergeOperations = 2,
			Operations =
			[
				new MergeOperationSummary
				{
					OperationNumber = 1,
					FilePath1 = TestPaths.Rooted("repo", "alpha", "file.txt"),
					FilePath2 = TestPaths.Rooted("repo", "beta", "file.txt"),
					SimilarityScore = 75,
					FilesAffected = 2,
					ConflictsResolved = 3,
					MergedLineCount = 40,
				},
				new MergeOperationSummary
				{
					OperationNumber = 2,
					FilePath1 = TestPaths.Rooted("repo", "beta", "file.txt"),
					FilePath2 = TestPaths.Rooted("repo", "gamma", "file.txt"),
					SimilarityScore = 90,
					FilesAffected = 4,
					ConflictsResolved = 0,
					MergedLineCount = 42,
				},
			],
		};

		ProgressReportingService.ShowDetailedMergeSummary(result);

		StringAssert.Contains(Output, "Merge Operations Summary");
		StringAssert.Contains(LineContaining("Initial File Groups"), "3");
		StringAssert.Contains(LineContaining("Total Files Processed"), "6");
		StringAssert.Contains(Output.Split('\n').First(line => line.Contains("Merge Operations", StringComparison.Ordinal) && !line.Contains("Summary", StringComparison.Ordinal)), "2");
		StringAssert.Contains(LineContaining("Final Result"), "Success");
		StringAssert.Contains(LineContaining("Final Line Count"), "42");
		StringAssert.Contains(Output, "Individual Operations:");

		string firstOperation = LineContaining("alpha/file.txt ↔ beta/file.txt");
		StringAssert.Contains(firstOperation, $"{75.0:F1}%");
		StringAssert.Contains(firstOperation, "40");
		string secondOperation = LineContaining("beta/file.txt ↔ gamma/file.txt");
		StringAssert.Contains(secondOperation, $"{90.0:F1}%");

		StringAssert.Contains(Output, "Successfully merged 3 file groups into a single result through 2 operations.");
	}

	/// <summary>
	/// A failed summary says how many operations ran before it stopped, and omits a zero line count.
	/// </summary>
	[TestMethod]
	public void ShowDetailedMergeSummary_FailedWithOperations_ReportsStop()
	{
		MergeCompletionResult result = new(false, null, 0, "cancelled")
		{
			InitialFileGroups = 2,
			TotalFilesMerged = 2,
			TotalMergeOperations = 1,
			Operations =
			[
				new MergeOperationSummary
				{
					OperationNumber = 1,
					FilePath1 = TestPaths.Rooted("one", "x.txt"),
					FilePath2 = TestPaths.Rooted("two", "x.txt"),
					SimilarityScore = 10,
				},
			],
		};

		ProgressReportingService.ShowDetailedMergeSummary(result);

		StringAssert.Contains(LineContaining("Final Result"), "Failed/Cancelled");
		Assert.DoesNotContain("Final Line Count", Output);
		StringAssert.Contains(Output, "Merge process was cancelled after 1 operations.");
		Assert.DoesNotContain("Successfully merged", Output);
	}

	/// <summary>
	/// A summary with no operations says every file was already identical.
	/// </summary>
	[TestMethod]
	public void ShowDetailedMergeSummary_NoOperations_ReportsAlreadySynchronized()
	{
		MergeCompletionResult result = new(true, "same", 5, "file.txt")
		{
			InitialFileGroups = 1,
			TotalFilesMerged = 4,
		};

		ProgressReportingService.ShowDetailedMergeSummary(result);

		StringAssert.Contains(Output, "No merge operations required - all files were already synchronized.");
		StringAssert.Contains(Output, "All 4 files were already identical - no merging required.");
		Assert.DoesNotContain("Individual Operations", Output);
	}

	/// <summary>
	/// A merge step success is reported.
	/// </summary>
	[TestMethod]
	public void ReportMergeStepSuccess_ReportsReduction()
	{
		ProgressReportingService.ReportMergeStepSuccess();

		StringAssert.Contains(Output, "Merged successfully! Versions reduced by 1.");
	}

	/// <summary>
	/// A merge with no counted changes has no statistics or bar, and lists both files.
	/// </summary>
	[TestMethod]
	public void ReportMergeInitiation_NoChanges_ListsBothFilesWithoutStatistics()
	{
		ProgressReportingService.ReportMergeInitiation(
			TestPaths.Rooted("repo", "left", "f.txt"),
			TestPaths.Rooted("repo", "right", "f.txt"),
			hasExistingContent: false);

		Assert.AreEqual(string.Empty, LineContaining("Merging").Split("Merging")[1].Trim().TrimEnd(':'));
		StringAssert.Contains(Output, "left/f.txt");
		StringAssert.Contains(Output, "right/f.txt");
		Assert.DoesNotContain("<existing merged content>", Output);
		StringAssert.Contains(Output, "Result will replace both files");
	}

	/// <summary>
	/// Small change counts are drawn one mark per change, and existing merged content is named as the left side.
	/// </summary>
	[TestMethod]
	public void ReportMergeInitiation_SmallCounts_DrawsOneMarkPerChange()
	{
		ProgressReportingService.ReportMergeInitiation(
			TestPaths.Rooted("repo", "left", "f.txt"),
			TestPaths.Rooted("repo", "right", "f.txt"),
			hasExistingContent: true,
			deletions: 2,
			insertions: 3);

		StringAssert.Contains(LineContaining("Merging"), $"({DeletionMark}2 +3)");
		Assert.AreEqual($"{DeletionMark}{DeletionMark}+++", TugOfWarBar());
		StringAssert.Contains(Output, "<existing merged content> → left/f.txt");
	}

	/// <summary>
	/// Insertions alone show only the insertion count and insertion marks.
	/// </summary>
	[TestMethod]
	public void ReportMergeInitiation_InsertionsOnly_ShowsOnlyInsertions()
	{
		ProgressReportingService.ReportMergeInitiation("a.txt", "b.txt", hasExistingContent: false, insertions: 4);

		StringAssert.Contains(LineContaining("Merging"), "(+4)");
		Assert.AreEqual("++++", TugOfWarBar());
	}

	/// <summary>
	/// Large change counts are scaled to fit, keeping their proportion and at least one mark each.
	/// </summary>
	[TestMethod]
	public void ReportMergeInitiation_LargeCounts_ScalesBarProportionally()
	{
		ProgressReportingService.ReportMergeInitiation("a.txt", "b.txt", hasExistingContent: false, deletions: 5, insertions: 5000);

		string bar = TugOfWarBar();
		int deleteMarks = bar.Count(c => c == DeletionMark);
		int insertMarks = bar.Count(c => c == '+');
		Assert.AreEqual(bar.Length, deleteMarks + insertMarks);
		Assert.IsGreaterThanOrEqualTo(1, deleteMarks);
		Assert.IsGreaterThan(deleteMarks, insertMarks);
		Assert.IsLessThanOrEqualTo(100, bar.Length);
	}

	/// <summary>
	/// A large count on one side alone is capped to the available width.
	/// </summary>
	[TestMethod]
	public void ReportMergeInitiation_LargeDeletionsOnly_CapsBar()
	{
		ProgressReportingService.ReportMergeInitiation("a.txt", "b.txt", hasExistingContent: false, deletions: 5000);

		string bar = TugOfWarBar();
		Assert.AreEqual(bar.Length, bar.Count(c => c == DeletionMark));
		Assert.IsGreaterThanOrEqualTo(10, bar.Length);
		Assert.IsLessThanOrEqualTo(100, bar.Length);
	}
}
