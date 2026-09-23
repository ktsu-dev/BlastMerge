// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.Linq;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="FileDiffer.CalculateLineSimilarity"/>, which scores how alike two files are
/// and so decides which pair the iterative merge takes first.
/// </summary>
[TestClass]
public class FileDifferSimilarityTests
{
	/// <summary>
	/// The issue's own example: the same distinct lines on both sides, but one file repeats a line.
	/// Scoring the distinct values alone called these identical.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_RepeatedLineOnOneSideOnly_IsNotIdentical()
	{
		string[] lines1 = ["X", "X", "X", "Y"];
		string[] lines2 = ["X", "Y"];

		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		// Two lines match; the union holds four. Whatever the exact ratio, these are not identical.
		Assert.IsTrue(similarity < 1.0, $"Files of different length should not score 1.0, got {similarity}");
		Assert.AreEqual(0.5, similarity, 0.001, "Two matching lines out of a four-line union");
	}

	/// <summary>
	/// The count has to be taken from the scarcer side, not just detected as present on both.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_RepeatCountsDiffer_MatchesOnlyTheScarcerSide()
	{
		string[] lines1 = ["A", "A", "A", "A"];
		string[] lines2 = ["A", "A"];

		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		// min(4,2) = 2 match, union = 4 + 2 - 2 = 4.
		Assert.AreEqual(0.5, similarity, 0.001, "Two of four repeats should match");
	}

	/// <summary>
	/// A file that is purely one line repeated is not the same as a file that is that line once,
	/// however many times it is repeated.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_LengthDifferenceGrows_ScoreFalls()
	{
		string[] shortFile = ["A"];
		double similarityToTwo = FileDiffer.CalculateLineSimilarity(["A", "A"], shortFile);
		double similarityToTen = FileDiffer.CalculateLineSimilarity([.. Enumerable.Repeat("A", 10)], shortFile);

		Assert.IsTrue(similarityToTen < similarityToTwo,
			$"Ten repeats should score below two repeats, got {similarityToTen} and {similarityToTwo}");
	}

	/// <summary>
	/// Duplicate lines inside a file must not make it look less like an identical copy of itself.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_IdenticalFilesWithRepeatedLines_ReturnsOne()
	{
		string[] lines1 = ["A", "B", "B", "C", "B"];
		string[] lines2 = ["A", "B", "B", "C", "B"];

		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		Assert.AreEqual(1.0, similarity, 0.001, "A file should be identical to a copy of itself");
	}

	/// <summary>
	/// Where no line repeats, the multiset is the set, so the long-standing scores are unchanged.
	/// This is the regression guard for every caller that was already scoring correctly.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_NoRepeatedLines_ScoresAsBefore()
	{
		string[] lines1 = ["A", "B", "C"];
		string[] lines2 = ["B", "C", "D"];

		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		// Two lines in common, four distinct across both: the set-based answer, unchanged.
		Assert.AreEqual(0.5, similarity, 0.001, "Files without repeats should score exactly as they did");
	}

	/// <summary>
	/// Files sharing no line at all still score zero.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_NoLinesInCommon_ReturnsZero()
	{
		string[] lines1 = ["A", "A", "B"];
		string[] lines2 = ["C", "D", "D"];

		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		Assert.AreEqual(0.0, similarity, 0.001, "Files with no line in common should score 0.0");
	}
}
