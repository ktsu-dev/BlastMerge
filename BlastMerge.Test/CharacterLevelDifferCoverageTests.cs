// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers how <see cref="CharacterLevelDiffer.CreateSideBySideCharacterDiff"/> makes whitespace
/// visible without disturbing the markup tags it is wrapped in.
/// </summary>
[TestClass]
public class CharacterLevelDifferCoverageTests
{
	/// <summary>
	/// Two empty lines give two empty sides.
	/// </summary>
	[TestMethod]
	public void CreateSideBySideCharacterDiff_EmptyLines_GiveEmptySides()
	{
		(string left, string right) = CharacterLevelDiffer.CreateSideBySideCharacterDiff(string.Empty, string.Empty);

		Assert.AreEqual(string.Empty, left);
		Assert.AreEqual(string.Empty, right);
	}

	/// <summary>
	/// A tab in the content is shown as an arrow while the tag around it keeps its own space.
	/// </summary>
	[TestMethod]
	public void CreateSideBySideCharacterDiff_TabIsShownAsArrow()
	{
		(string left, string right) = CharacterLevelDiffer.CreateSideBySideCharacterDiff("a\tb", "a\tc");

		Assert.Contains("[dim white]→[/]", left);
		Assert.Contains("[dim white]→[/]", right);
		Assert.Contains("[red]b[/]", left);
		Assert.Contains("[green]c[/]", right);
		Assert.DoesNotContain("\t", left);
		Assert.DoesNotContain("\t", right);
	}

	/// <summary>
	/// Carriage returns and line feeds are shown as their symbols.
	/// </summary>
	[TestMethod]
	public void CreateSideBySideCharacterDiff_LineEndingsAreShownAsSymbols()
	{
		(string left, string right) = CharacterLevelDiffer.CreateSideBySideCharacterDiff("x\r", "x\n");

		Assert.Contains("[red]↵[/]", left);
		Assert.Contains("[green]¶[/]", right);
		Assert.DoesNotContain("\r", left);
		Assert.DoesNotContain("\n", right);
	}

	/// <summary>
	/// Spaces in the content become middle dots, but the space inside a tag name is left alone.
	/// </summary>
	[TestMethod]
	public void CreateSideBySideCharacterDiff_SpacesInContentOnly()
	{
		(string left, _) = CharacterLevelDiffer.CreateSideBySideCharacterDiff("a b", "a c");

		Assert.StartsWith("[dim white]a[/][dim white]·[/]", left);
	}
}
