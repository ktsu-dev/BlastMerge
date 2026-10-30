// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.IO;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for how the option chosen on a replace block maps back to a <see cref="BlockChoice"/>.
/// </summary>
[TestClass]
public class ReplaceChoiceMappingTests
{
	private static BlockChoice Choose(int optionIndex, string leftLabel, string rightLabel)
	{
		string[] options = ConsoleApplicationService.GetReplaceChoiceOptions(leftLabel, rightLabel);
		return ConsoleApplicationService.MapReplaceChoice(options[optionIndex], leftLabel, rightLabel);
	}

	[TestMethod]
	public void EachOptionMapsToItsOwnChoice()
	{
		Assert.AreEqual(BlockChoice.UseVersion1, Choose(0, "left/config.json", "right/config.json"));
		Assert.AreEqual(BlockChoice.UseVersion2, Choose(1, "left/config.json", "right/config.json"));
		Assert.AreEqual(BlockChoice.UseBoth, Choose(2, "left/config.json", "right/config.json"));
		Assert.AreEqual(BlockChoice.Skip, Choose(3, "left/config.json", "right/config.json"));
	}

	[TestMethod]
	public void SkipBothIsNotReadAsUseBoth()
	{
		// #108: "❌ Skip Both" contains "Both", so it used to map to UseBoth.
		Assert.AreEqual(BlockChoice.Skip, Choose(3, "a/settings.txt", "b/settings.txt"));
	}

	[TestMethod]
	public void RightLabelThatStartsWithTheLeftLabelPicksTheRightVersion()
	{
		// #113: "  Use Dockerfiles/Dockerfile" contains "Use Dockerfile", so it used to map to UseVersion1.
		Assert.AreEqual(BlockChoice.UseVersion2, Choose(1, "Dockerfile", "Dockerfiles/Dockerfile"));
		Assert.AreEqual(BlockChoice.UseVersion1, Choose(0, "Dockerfile", "Dockerfiles/Dockerfile"));
	}

	[TestMethod]
	public void LabelsFromRealPathsMapTheRightOptionToTheRightVersion()
	{
		string root = Path.Join(Path.GetTempPath(), "repo");
		string[][] pairs =
		[
			[Path.Join(root, "Dockerfile"), Path.Join(root, "Dockerfiles", "Dockerfile")],
			[Path.Join(root, "LICENSE"), Path.Join(root, "LICENSES", "LICENSE")],
			[Path.Join(root, "Makefile"), Path.Join(root, "Makefile.d", "Makefile")],
		];

		foreach (string[] pair in pairs)
		{
			(string leftLabel, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(pair[0], pair[1]);
			Assert.AreEqual(BlockChoice.UseVersion2, Choose(1, leftLabel, rightLabel), $"{leftLabel} / {rightLabel}");
			Assert.AreEqual(BlockChoice.UseVersion1, Choose(0, leftLabel, rightLabel), $"{leftLabel} / {rightLabel}");
		}
	}

	[TestMethod]
	public void ALabelContainingBothStillPicksItsOwnVersion()
	{
		Assert.AreEqual(BlockChoice.UseVersion1, Choose(0, "Both/notes.md", "Other/notes.md"));
		Assert.AreEqual(BlockChoice.Skip, Choose(3, "Both/notes.md", "Other/notes.md"));
	}
}
