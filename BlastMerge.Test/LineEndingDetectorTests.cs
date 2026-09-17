// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="LineEndingDetector"/>.
/// </summary>
/// <remarks>
/// These cases operate purely on in-memory strings, so they deliberately do not use a mock file
/// system. They pin the two properties the merge pipeline depends on: that the style a file used is
/// recoverable from its raw content, and that splitting content into lines never leaves a line
/// terminator stuck on the end of a line.
/// </remarks>
[TestClass]
public class LineEndingDetectorTests
{
	[TestMethod]
	public void Detect_WithCrlfContent_ReturnsCrlf() =>
		Assert.AreEqual("\r\n", LineEndingDetector.Detect("a\r\nb\r\nc"));

	[TestMethod]
	public void Detect_WithLfContent_ReturnsLf() =>
		Assert.AreEqual("\n", LineEndingDetector.Detect("a\nb\nc"));

	[TestMethod]
	public void Detect_WithCarriageReturnOnlyContent_ReturnsCarriageReturn() =>
		Assert.AreEqual("\r", LineEndingDetector.Detect("a\rb\rc"));

	[TestMethod]
	public void Detect_WithMixedContent_ReturnsTheDominantStyle() =>
		Assert.AreEqual(
			"\r\n",
			LineEndingDetector.Detect("a\r\nb\r\nc\nd"),
			"Two CRLF against one LF should resolve to CRLF");

	[TestMethod]
	public void Detect_WithCrlfContent_DoesNotCountTheLineFeedTwice() =>
		Assert.AreEqual(
			"\r\n",
			LineEndingDetector.Detect("a\r\nb"),
			"The line feed of a CRLF must not also register as a lone line feed");

	[TestMethod]
	public void Detect_WithNoLineEnding_FallsBackToThePlatformDefault() =>
		Assert.AreEqual(
			Environment.NewLine,
			LineEndingDetector.Detect("single line"),
			"There is no style to preserve, so new lines should use the platform default");

	[TestMethod]
	public void Detect_AcrossTwoSides_CountsBothSidesTogether() =>
		Assert.AreEqual(
			"\r\n",
			LineEndingDetector.Detect("a\nb", "c\r\nd\r\ne"),
			"The side contributing more line endings should decide, not the side read first");

	[TestMethod]
	public void Detect_WithNullContent_ThrowsArgumentNullException() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => LineEndingDetector.Detect(null!));

	[TestMethod]
	public void SplitLines_WithCrlfContent_LeavesNoCarriageReturnOnAnyLine() =>
		CollectionAssert.AreEqual(
			new[] { "a", "b", "c" },
			LineEndingDetector.SplitLines("a\r\nb\r\nc"),
			"Splitting CRLF content must not leave a stray carriage return on each line");

	[TestMethod]
	public void SplitLines_WithTrailingLineEnding_DoesNotEmitATrailingEmptyLine() =>
		CollectionAssert.AreEqual(
			new[] { "a", "b" },
			LineEndingDetector.SplitLines("a\r\nb\r\n"),
			"A trailing line ending terminates the last line rather than starting a new one");

	[TestMethod]
	public void SplitLines_WithEmptyContent_ReturnsNoLines() =>
		Assert.AreEqual(0, LineEndingDetector.SplitLines("").Length);

	[TestMethod]
	public void SplitLines_WithOnlyALineEnding_ReturnsOneEmptyLine() =>
		CollectionAssert.AreEqual(new[] { "" }, LineEndingDetector.SplitLines("\n"));

	[TestMethod]
	public void SplitLines_WithMixedLineEndings_SplitsOnEveryStyle() =>
		CollectionAssert.AreEqual(
			new[] { "a", "b", "c", "d" },
			LineEndingDetector.SplitLines("a\r\nb\nc\rd"));

	[TestMethod]
	public void SplitLines_MatchesReadAllLinesForContentWithoutATrailingLineEnding() =>
		CollectionAssert.AreEqual(
			new[] { "a", "b" },
			LineEndingDetector.SplitLines("a\nb"));

	[TestMethod]
	public void SplitLines_WithNullContent_ThrowsArgumentNullException() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => LineEndingDetector.SplitLines(null!));

	[TestMethod]
	public void Join_UsesTheGivenLineEnding() =>
		Assert.AreEqual("a\r\nb", LineEndingDetector.Join(["a", "b"], "\r\n"));

	[TestMethod]
	public void SplitLines_RoundTripsThroughJoin()
	{
		// Arrange
		string original = "alpha\r\nbeta\r\ngamma";

		// Act
		string roundTripped = LineEndingDetector.Join(
			LineEndingDetector.SplitLines(original),
			LineEndingDetector.Detect(original));

		// Assert
		Assert.AreEqual(original, roundTripped, "Reading and rewriting content should not change it");
	}
}
