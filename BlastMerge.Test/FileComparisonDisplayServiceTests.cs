// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.Linq;
using DiffBlock = DiffPlex.Model.DiffBlock;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

/// <summary>
/// Tests for <see cref="FileComparisonDisplayService"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class FileComparisonDisplayServiceTests : ConsoleTestBase
{
	private const int ChangeSummaryChoice = 0;
	private const int GitStyleChoice = 1;
	private const int SideBySideChoice = 2;
	private const int SkipChoice = 3;

	/// <summary>
	/// Gets the output written after the diff format prompt was answered, which excludes the
	/// prompt's own choice labels.
	/// </summary>
	private string AfterPrompt
	{
		get
		{
			const string lastChoice = "Skip comparison";
			int index = Output.LastIndexOf(lastChoice, StringComparison.Ordinal);
			return index < 0 ? Output : Output[(index + lastChoice.Length)..];
		}
	}

	/// <summary>
	/// Writes two numbered files of <paramref name="lineCount"/> lines, applying <paramref name="edit"/> to the second.
	/// </summary>
	/// <param name="lineCount">The number of lines in each file.</param>
	/// <param name="edit">Rewrites a line of the second file given its zero-based index.</param>
	/// <returns>The paths of the two files.</returns>
	private (string Left, string Right) WriteNumberedPair(int lineCount, Func<int, string, string> edit)
	{
		string[] lines = [.. Enumerable.Range(1, lineCount).Select(i => $"line{i}")];
		string[] edited = [.. lines.Select((line, index) => edit(index, line))];
		string left = WriteFile(Path.Join("left", "data.txt"), string.Join(Environment.NewLine, lines) + Environment.NewLine);
		string right = WriteFile(Path.Join("right", "data.txt"), string.Join(Environment.NewLine, edited) + Environment.NewLine);
		return (left, right);
	}

	/// <summary>
	/// Identical files are reported as identical without prompting for a format.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_IdenticalFiles_ReportsIdenticalWithoutPrompting()
	{
		string left = WriteFile("a.txt", "same\ncontent\n");
		string right = WriteFile("b.txt", "same\ncontent\n");

		FileComparisonDisplayService.CompareTwoFiles(left, right);

		StringAssert.Contains(Output, "Files are identical!");
		Assert.DoesNotContain("Choose diff format", Output);
	}

	/// <summary>
	/// Different files show the whitespace legend and the format prompt, and skipping shows no diff.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_DifferentFiles_SkipShowsLegendButNoDiff()
	{
		string left = WriteFile("a.txt", "alpha\nbeta\n");
		string right = WriteFile("b.txt", "alpha\ngamma\n");
		SelectIndex(SkipChoice);

		FileComparisonDisplayService.CompareTwoFiles(left, right);

		StringAssert.Contains(Output, "Files are different.");
		StringAssert.Contains(Output, "Whitespace Visualization Legend");
		Assert.DoesNotContain("Change Summary", AfterPrompt);
		Assert.DoesNotContain("Git-style Diff", AfterPrompt);
		Assert.DoesNotContain("Side-by-Side Diff", AfterPrompt);
	}

	/// <summary>
	/// Choosing the change summary counts the added line.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_ChangeSummaryChoice_CountsAdditions()
	{
		string left = WriteFile("a.txt", "alpha\nbeta\n");
		string right = WriteFile("b.txt", "alpha\nbeta\ngamma\n");
		SelectIndex(ChangeSummaryChoice);

		FileComparisonDisplayService.CompareTwoFiles(left, right);

		StringAssert.Contains(AfterPrompt, "Change Summary");
		StringAssert.Contains(AfterPrompt, "Found 1 differences");
		StringAssert.Contains(AfterPrompt, "+ Additions: 1");
		StringAssert.Contains(AfterPrompt, "- Deletions: 0");
	}

	/// <summary>
	/// Choosing the git-style diff renders the diff panel with both file headers.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_GitStyleChoice_RendersGitStyleDiff()
	{
		string left = WriteFile("a.txt", "alpha\nbeta\n");
		string right = WriteFile("b.txt", "alpha\nbeta\ngamma\n");
		SelectIndex(GitStyleChoice);

		FileComparisonDisplayService.CompareTwoFiles(left, right);

		StringAssert.Contains(AfterPrompt, "Git-style Diff");
		StringAssert.Contains(AfterPrompt, "+gamma");
		StringAssert.Contains(AfterPrompt, "--- ");
		StringAssert.Contains(AfterPrompt, "+++ ");
	}

	/// <summary>
	/// Choosing the side-by-side diff renders the side-by-side panel and its legend.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_SideBySideChoice_RendersSideBySideDiff()
	{
		(string left, string right) = WriteNumberedPair(3, (index, line) => index == 1 ? "changed" : line);
		SelectIndex(SideBySideChoice);

		FileComparisonDisplayService.CompareTwoFiles(left, right);

		StringAssert.Contains(AfterPrompt, "Side-by-Side Diff");
		StringAssert.Contains(AfterPrompt, "- line2");
		StringAssert.Contains(AfterPrompt, "+ changed");
		StringAssert.Contains(AfterPrompt, "Legend─");
	}

	/// <summary>
	/// A missing file is reported as an error rather than thrown.
	/// </summary>
	[TestMethod]
	public void CompareTwoFiles_MissingFile_ReportsFileNotFound()
	{
		string left = WriteFile("a.txt", "alpha\n");
		string missing = Path.Join(TempDirectory, "missing.txt");

		FileComparisonDisplayService.CompareTwoFiles(left, missing);

		StringAssert.Contains(Output, "File not found:");
	}

	/// <summary>
	/// The change summary counts additions, deletions and modifications separately.
	/// </summary>
	[TestMethod]
	public void ShowChangeSummary_DifferentFiles_ShowsTotalCount()
	{
		string left = WriteFile("a.txt", "one\ntwo\nthree\n");
		string right = WriteFile("b.txt", "one\nthree\nfour\n");
		int expected = ktsu.BlastMerge.Services.FileDiffer.FindDifferences(left, right).Count;

		FileComparisonDisplayService.ShowChangeSummary(left, right);

		StringAssert.Contains(Output, $"Found {expected} differences");
		StringAssert.Contains(Output, "~ Modifications:");
	}

	/// <summary>
	/// The change summary of identical files says so instead of drawing a panel.
	/// </summary>
	[TestMethod]
	public void ShowChangeSummary_IdenticalFiles_ReportsIdentical()
	{
		string left = WriteFile("a.txt", "x\n");
		string right = WriteFile("b.txt", "x\n");

		FileComparisonDisplayService.ShowChangeSummary(left, right);

		StringAssert.Contains(Output, "Files are identical!");
		Assert.DoesNotContain("Change Summary", Output);
	}

	/// <summary>
	/// A git-style diff of a missing file reports the error.
	/// </summary>
	[TestMethod]
	public void ShowGitStyleDiff_MissingFile_ReportsFileNotFound()
	{
		string left = WriteFile("a.txt", "alpha\n");

		FileComparisonDisplayService.ShowGitStyleDiff(left, Path.Join(TempDirectory, "missing.txt"));

		StringAssert.Contains(Output, "File not found:");
		Assert.DoesNotContain("Git-style Diff", Output);
	}

	/// <summary>
	/// A similar deleted and added pair is rendered as one character-level line, and a dissimilar
	/// deletion keeps its own line.
	/// </summary>
	[TestMethod]
	public void ShowGitStyleDiff_SimilarAndDissimilarChanges_RendersBoth()
	{
		string left = WriteFile("a.txt", "keep\nhelloworld\nobsolete\n");
		string right = WriteFile("b.txt", "keep\nhelloworld2\n");

		FileComparisonDisplayService.ShowGitStyleDiff(left, right);

		StringAssert.Contains(Output, "Git-style Diff");
		StringAssert.Contains(Output, " keep");
		StringAssert.Contains(Output, "-obsolete");
		StringAssert.Contains(Output, "helloworld");
	}

	/// <summary>
	/// Unchanged lines, file headers and chunk headers that contain markup characters are printed
	/// literally instead of being parsed as Spectre markup.
	/// </summary>
	[TestMethod]
	public void ShowGitStyleDiff_UnchangedLinesAndPathsWithBrackets_AreRenderedLiterally()
	{
		string left = WriteFile("[v1]a.cs", "[TestMethod]\nint[] values = [];\nold\n");
		string right = WriteFile("[v2]a.cs", "[TestMethod]\nint[] values = [];\nnew\n");

		FileComparisonDisplayService.ShowGitStyleDiff(left, right);

		StringAssert.Contains(Output, "Git-style Diff");
		StringAssert.Contains(Output, " [TestMethod]");
		StringAssert.Contains(Output, " int[] values = [];");
		StringAssert.Contains(Output, "[v1]a.cs");
		StringAssert.Contains(Output, "[v2]a.cs");
		StringAssert.Contains(Output, "-old");
		StringAssert.Contains(Output, "+new");
	}

	/// <summary>
	/// A side-by-side diff of identical files reports them identical without a table.
	/// </summary>
	[TestMethod]
	public void ShowSideBySideDiff_IdenticalFiles_ReportsIdentical()
	{
		string left = WriteFile("a.txt", "x\ny\n");
		string right = WriteFile("b.txt", "x\ny\n");

		FileComparisonDisplayService.ShowSideBySideDiff(left, right);

		StringAssert.Contains(Output, "Files are identical!");
		Assert.DoesNotContain("Side-by-Side Diff", Output);
	}

	/// <summary>
	/// A side-by-side diff labels each column with the distinguishing part of its path and shows
	/// the added, deleted and modified counts in its header.
	/// </summary>
	[TestMethod]
	public void ShowSideBySideDiff_TwoSeparateChanges_ShowsLabelsAndStatistics()
	{
		(string left, string right) = WriteNumberedPair(20, (index, line) => index is 1 or 17 ? $"edited{index}" : line);

		FileComparisonDisplayService.ShowSideBySideDiff(left, right);

		StringAssert.Contains(Output, "Side-by-Side Diff");
		StringAssert.Contains(Output, "left/data.txt");
		StringAssert.Contains(Output, "right/data.txt");
		StringAssert.Contains(Output, "+2 -2 ~2");
		StringAssert.Contains(Output, "- line2");
		StringAssert.Contains(Output, "+ edited1");
		StringAssert.Contains(Output, "- line18");
		StringAssert.Contains(Output, "+ edited17");

		// Context is three lines either side, so lines far from both changes are not shown.
		Assert.DoesNotContain("line10", Output);
	}

	/// <summary>
	/// A pure insertion has no modification count in the side-by-side header.
	/// </summary>
	[TestMethod]
	public void ShowSideBySideDiff_PureInsertion_OmitsModificationCount()
	{
		string left = WriteFile("a.txt", "one\ntwo\n");
		string right = WriteFile("b.txt", "one\ntwo\nthree\n");

		FileComparisonDisplayService.ShowSideBySideDiff(left, right);

		StringAssert.Contains(Output, "+1 -0");
		Assert.DoesNotContain("~", Output.Split('\n').First(line => line.Contains("Side-by-Side Diff", StringComparison.Ordinal)));
		StringAssert.Contains(Output, "+ three");
	}

	/// <summary>
	/// A side-by-side diff of a missing file reports the error.
	/// </summary>
	[TestMethod]
	public void ShowSideBySideDiff_MissingFile_ReportsError()
	{
		string left = WriteFile("a.txt", "x\n");

		FileComparisonDisplayService.ShowSideBySideDiff(left, Path.Join(TempDirectory, "missing.txt"));

		StringAssert.Contains(Output, "File not found:");
	}

	/// <summary>
	/// Reading a directory as a file is reported as an error rather than thrown.
	/// </summary>
	[TestMethod]
	public void ShowSideBySideDiff_DirectoryInsteadOfFile_ReportsError()
	{
		string left = WriteFile("a.txt", "x\n");
		string directory = Path.Join(TempDirectory, "folder");
		Directory.CreateDirectory(directory);

		FileComparisonDisplayService.ShowSideBySideDiff(left, directory);

		Assert.IsTrue(
			Output.Contains("IO error:", StringComparison.Ordinal) || Output.Contains("Access denied:", StringComparison.Ordinal),
			$"Expected an IO or access error, got: {Output}");
	}

	/// <summary>
	/// Every public entry point rejects null paths.
	/// </summary>
	[TestMethod]
	public void PublicMethods_NullPaths_Throw()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.CompareTwoFiles(null!, "b"));
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.CompareTwoFiles("a", null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.ShowChangeSummary(null!, "b"));
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.ShowGitStyleDiff("a", null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.ShowSideBySideDiff(null!, "b"));
		Assert.ThrowsExactly<ArgumentNullException>(() => FileComparisonDisplayService.GetDiffBlockStatistics(null!));
	}

	/// <summary>
	/// The diff block table has a row for each context line and each changed line, with the
	/// longer side of each section setting its height.
	/// </summary>
	[TestMethod]
	public void CreateSideBySideDiffTable_BuildsContextAndChangeRows()
	{
		string[] lines1 = ["first", "old", "last"];
		string[] lines2 = ["first", "new", "added", "last", "tail"];
		DiffBlock block = new(1, 1, 1, 2);
		BlockContext context = BlockContext.Create(["first"], ["last"], ["first"], ["last", "tail"]);

		Table table = FileComparisonDisplayService.CreateSideBySideDiffTable(lines1, lines2, block, context, "Mine", "Theirs");

		Assert.HasCount(3, table.Columns);
		Assert.AreEqual(5, table.Rows.Count);
	}

	/// <summary>
	/// Rendering the diff block table numbers rows by the left file, falling back to the right file
	/// for rows with no left line, and marks deleted and inserted lines.
	/// </summary>
	[TestMethod]
	public void ShowDiffBlock_RendersNumberedMarkedRows()
	{
		string[] lines1 = ["first", "old", "last"];
		string[] lines2 = ["first", "new", "added", "last"];
		DiffBlock block = new(1, 1, 1, 2);
		BlockContext context = BlockContext.Create(["first"], ["last"], ["first"], ["last"]);

		FileComparisonDisplayService.ShowDiffBlock(lines1, lines2, block, context, "Mine", "Theirs");

		string[] rendered = Output.Split('\n');
		StringAssert.Contains(Output, "Mine");
		StringAssert.Contains(Output, "Theirs");
		string firstChange = rendered.Single(line => line.Contains("- old", StringComparison.Ordinal));
		StringAssert.Contains(firstChange, "+ new");
		StringAssert.Contains(firstChange.TrimStart('│', ' '), "2");
		string secondChange = rendered.Single(line => line.Contains("+ added", StringComparison.Ordinal));
		Assert.StartsWith("3", secondChange.TrimStart('│', ' '));
		string before = rendered.Single(line => line.Contains("first", StringComparison.Ordinal));
		Assert.StartsWith("1", before.TrimStart('│', ' '));
		string after = rendered.Single(line => line.Contains("last", StringComparison.Ordinal));
		Assert.StartsWith("3", after.TrimStart('│', ' '));
	}

	/// <summary>
	/// Similar changed lines are highlighted character by character on both sides.
	/// </summary>
	[TestMethod]
	public void ShowDiffBlock_SimilarLines_UsesCharacterHighlighting()
	{
		string[] lines1 = ["helloworld"];
		string[] lines2 = ["helloworld2"];
		DiffBlock block = new(0, 1, 0, 1);
		BlockContext context = BlockContext.Create([], [], [], []);

		FileComparisonDisplayService.ShowDiffBlock(lines1, lines2, block, context, "Mine", "Theirs");

		string row = Output.Split('\n').Single(line => line.Contains("- ", StringComparison.Ordinal) && line.Contains("+ ", StringComparison.Ordinal));
		StringAssert.Contains(row, "hello");
		StringAssert.Contains(row, "2");
	}

	/// <summary>
	/// The block statistics are the block's deletion and insertion counts.
	/// </summary>
	[TestMethod]
	public void GetDiffBlockStatistics_ReturnsDeleteAndInsertCounts()
	{
		(int deletions, int insertions) = FileComparisonDisplayService.GetDiffBlockStatistics(new DiffBlock(4, 3, 2, 5));

		Assert.AreEqual(3, deletions);
		Assert.AreEqual(5, insertions);
	}
}
