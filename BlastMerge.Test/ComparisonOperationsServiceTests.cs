// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using ktsu.BlastMerge.Cli.Services.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="ComparisonOperationsService"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ComparisonOperationsServiceTests : ConsoleTestBase
{
	private string left = string.Empty;
	private string right = string.Empty;

	/// <summary>
	/// Creates two directories with an identical file, a modified file, and a file unique to each.
	/// </summary>
	[TestInitialize]
	public void CreateDirectories()
	{
		left = Path.Join(TempDirectory, "left");
		right = Path.Join(TempDirectory, "right");
		WriteFile(Path.Join("left", "same.txt"), "identical\n");
		WriteFile(Path.Join("right", "same.txt"), "identical\n");
		WriteFile(Path.Join("left", "changed.txt"), "one\ntwo\nthree\n");
		WriteFile(Path.Join("right", "changed.txt"), "one\nTWO\nthree\nfour\n");
		WriteFile(Path.Join("left", "leftonly.txt"), "left");
		WriteFile(Path.Join("right", "rightonly.txt"), "right");
		WriteFile(Path.Join("left", "nested", "deep.txt"), "deep");
	}

	/// <summary>
	/// Comparing directories counts each category of file.
	/// </summary>
	[TestMethod]
	public void CompareDirectories_ReportsEachCategory()
	{
		Input.PushTextWithEnter("n");

		ComparisonOperationsService.CompareDirectories(left, right, "*.txt", recursive: false);

		StringAssert.Contains(Output, "Identical Files");
		StringAssert.Contains(Output, "same.txt");
		StringAssert.Contains(Output, "Modified Files");
		StringAssert.Contains(Output, "changed.txt");
		StringAssert.Contains(Output, "leftonly.txt");
		StringAssert.Contains(Output, "rightonly.txt");
		Assert.DoesNotContain("deep.txt", Output);
	}

	/// <summary>
	/// A recursive comparison includes files in subdirectories.
	/// </summary>
	[TestMethod]
	public void CompareDirectories_Recursive_IncludesSubdirectories()
	{
		Input.PushTextWithEnter("n");

		ComparisonOperationsService.CompareDirectories(left, right, "*.txt", recursive: true);

		StringAssert.Contains(Output, "deep.txt");
	}

	/// <summary>
	/// Asking for detail on modified files shows a change summary for each.
	/// </summary>
	[TestMethod]
	public void CompareDirectories_DetailRequested_ShowsChangeSummary()
	{
		Input.PushTextWithEnter("y");

		ComparisonOperationsService.CompareDirectories(left, right, "*.txt", recursive: false);

		StringAssert.Contains(Output, "Differences: changed.txt");
		StringAssert.Contains(Output, "Change Summary");
	}

	/// <summary>
	/// Declining detail shows only the summary table.
	/// </summary>
	[TestMethod]
	public void CompareDirectories_DetailDeclined_ShowsOnlyTable()
	{
		Input.PushTextWithEnter("n");

		ComparisonOperationsService.CompareDirectories(left, right, "*.txt", recursive: false);

		StringAssert.Contains(Output, "Modified Files");
		Assert.DoesNotContain("Change Summary", Output);
	}

	/// <summary>
	/// With no modified files the user is not asked about detail.
	/// </summary>
	[TestMethod]
	public void CompareDirectories_NoModifiedFiles_DoesNotPrompt()
	{
		ComparisonOperationsService.CompareDirectories(left, right, "same.txt", recursive: false);

		StringAssert.Contains(Output, "Identical Files");
		Assert.DoesNotContain("Show detailed differences", Output);
	}

	/// <summary>
	/// The interactive directory comparison asks for both directories, a pattern and recursion, then waits.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoDirectories_ComparesEnteredDirectories()
	{
		Input.PushTextWithEnter(left);
		Input.PushTextWithEnter(right);
		Input.PushTextWithEnter("*.txt");
		Input.PushTextWithEnter("y"); // recursive
		Input.PushTextWithEnter("n"); // no detail
		PressAnyKey();

		ComparisonOperationsService.HandleCompareTwoDirectories();

		StringAssert.Contains(Output, "Compare Two Directories");
		StringAssert.Contains(Output, "deep.txt");
		StringAssert.Contains(Output, "Press any key to continue...");
	}

	/// <summary>
	/// Leaving the first directory empty cancels.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoDirectories_EmptyFirst_Cancels()
	{
		Input.PushKey(ConsoleKey.Enter);

		ComparisonOperationsService.HandleCompareTwoDirectories();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("second directory", Output);
	}

	/// <summary>
	/// A first directory that does not exist is reported before the second is asked for.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoDirectories_MissingFirst_ReportsError()
	{
		Input.PushTextWithEnter(Path.Join(TempDirectory, "missing"));

		ComparisonOperationsService.HandleCompareTwoDirectories();

		StringAssert.Contains(Output, "First directory does not exist!");
		Assert.DoesNotContain("second directory", Output);
	}

	/// <summary>
	/// Leaving the second directory empty cancels.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoDirectories_EmptySecond_Cancels()
	{
		Input.PushTextWithEnter(left);
		Input.PushKey(ConsoleKey.Enter);

		ComparisonOperationsService.HandleCompareTwoDirectories();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("file pattern", Output);
	}

	/// <summary>
	/// A second directory that does not exist is reported.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoDirectories_MissingSecond_ReportsError()
	{
		Input.PushTextWithEnter(left);
		Input.PushTextWithEnter(Path.Join(TempDirectory, "missing"));

		ComparisonOperationsService.HandleCompareTwoDirectories();

		StringAssert.Contains(Output, "Second directory does not exist!");
		Assert.DoesNotContain("file pattern", Output);
	}

	/// <summary>
	/// Two identical files are reported as identical.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_IdenticalFiles_ReportsIdentical()
	{
		Input.PushTextWithEnter(Path.Join(left, "same.txt"));
		Input.PushTextWithEnter(Path.Join(right, "same.txt"));
		PressAnyKey();

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "Compare Two Specific Files");
		StringAssert.Contains(Output, "Files are identical!");
	}

	/// <summary>
	/// Two different files are reported as different, and the chosen diff format is shown.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_DifferentFiles_ShowsChosenDiff()
	{
		Input.PushTextWithEnter(Path.Join(left, "changed.txt"));
		Input.PushTextWithEnter(Path.Join(right, "changed.txt"));
		SelectIndex(0); // change summary
		PressAnyKey();

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "Files are different.");
		StringAssert.Contains(Output, "Change Summary");
	}

	/// <summary>
	/// Leaving the first file empty cancels.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_EmptyFirst_Cancels()
	{
		Input.PushKey(ConsoleKey.Enter);

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("second file", Output);
	}

	/// <summary>
	/// A first file that does not exist is reported before the second is asked for.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_MissingFirst_ReportsError()
	{
		Input.PushTextWithEnter(Path.Join(TempDirectory, "missing.txt"));

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "First file does not exist!");
		Assert.DoesNotContain("second file", Output);
	}

	/// <summary>
	/// Leaving the second file empty cancels.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_EmptySecond_Cancels()
	{
		Input.PushTextWithEnter(Path.Join(left, "same.txt"));
		Input.PushKey(ConsoleKey.Enter);

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("identical", Output);
	}

	/// <summary>
	/// A second file that does not exist is reported.
	/// </summary>
	[TestMethod]
	public void HandleCompareTwoSpecificFiles_MissingSecond_ReportsError()
	{
		Input.PushTextWithEnter(Path.Join(left, "same.txt"));
		Input.PushTextWithEnter(Path.Join(TempDirectory, "missing.txt"));

		ComparisonOperationsService.HandleCompareTwoSpecificFiles();

		StringAssert.Contains(Output, "Second file does not exist!");
	}
}
