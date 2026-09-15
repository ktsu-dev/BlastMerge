// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="PathExclusionMatcher"/>, which decides whether a path is excluded by a
/// batch configuration's path exclusion patterns.
/// </summary>
/// <remarks>
/// Paths are given already normalized (rooted, <c>/</c> separators), which is the contract the
/// matcher is called under.
/// </remarks>
[TestClass]
public class PathExclusionMatcherTests
{
	// The escaping bug this matcher exists to fix: the previous implementation hand-built a regex
	// and escaped only *, ? and /, so every other metacharacter kept its regex meaning. In
	// "src/*.tmp" the dot matched any character, making "report.tmp" and "reportXtmp" equivalent.
	[TestMethod]
	public void Matches_DotInPattern_IsLiteralNotAnyCharacter()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/report.tmp", "/work/src/*.tmp"),
			"A literal dot should still match a literal dot.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/reportXtmp", "/work/src/*.tmp"),
			"The dot in the pattern must not match an arbitrary character.");
	}

	[TestMethod]
	public void Matches_SuffixPattern_DoesNotTreatDotAsWildcard()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/notes.txt", "*.txt"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/notesXtxt", "*.txt"),
			"'*.txt' must not match a name that merely ends in 'txt'.");
	}

	[TestMethod]
	public void Matches_RegexMetacharactersInPattern_AreLiteral()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/b+c/file.txt", "*/b+c/*"),
			"'+' should be a literal character in a path component.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/bbc/file.txt", "*/b+c/*"),
			"'+' must not be a regex repetition operator.");
	}

	// The previous implementation sliced pattern[1..^1] for anything starting and ending with '*',
	// which threw ArgumentOutOfRangeException for the single-character pattern "*" and aborted the
	// whole file search.
	[TestMethod]
	public void Matches_SingleAsteriskPattern_DoesNotThrow()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/file.txt", "*"));
	}

	[TestMethod]
	public void Matches_DirectoryPattern_MatchesAtAnyDepth()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/bin/file.txt", "*/bin/*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/a/b/bin/c/file.txt", "*/bin/*"),
			"'*/bin/*' has always matched a bin component at any depth.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/binaries/file.txt", "*/bin/*"),
			"A component merely starting with 'bin' is not a 'bin' component.");
	}

	[TestMethod]
	public void Matches_SubstringPattern_MatchesAnyComponent()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/node_modules/pkg/file.txt", "*node_modules*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/my_node_modules", "*node_modules*"),
			"The matching component may be the last one.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", "*node_modules*"));
	}

	[TestMethod]
	public void Matches_PrefixPattern_MatchesAnyComponent()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/tempdir/file.txt", "temp*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/tempfile", "temp*"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", "temp*"));
	}

	[TestMethod]
	public void Matches_PatternWithoutWildcards_IsAContainsCheck()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/generated/file.txt", "generated"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", "generated"));
	}

	[TestMethod]
	public void Matches_IsCaseInsensitive()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/BIN/file.txt", "*/bin/*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/bin/file.txt", "*/BIN/*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/TempDir/file.txt", "temp*"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/Generated/file.txt", "generated"));
	}

	[TestMethod]
	public void Matches_BackslashSeparatorsInPattern_AreTreatedAsPathSeparators()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/bin/file.txt", @"*\bin\*"),
			"Saved batch configurations store Windows-style patterns such as '*\\bin\\*'.");
	}

	[TestMethod]
	public void Matches_QuestionMark_MatchesExactlyOneCharacter()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/ab.txt", "a?.txt"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/abc.txt", "a?.txt"));
	}

	[TestMethod]
	public void Matches_BlankPattern_MatchesNothing()
	{
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", ""));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", "   "));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", null!));
	}

	[TestMethod]
	public void IsExcluded_NoPatterns_ExcludesNothing() =>
		Assert.IsFalse(PathExclusionMatcher.IsExcluded("/work/bin/file.txt", []));

	[TestMethod]
	public void IsExcluded_AnyPatternMatching_ExcludesThePath()
	{
		string[] patterns = ["*/bin/*", "*/obj/*", "*node_modules*"];

		Assert.IsTrue(PathExclusionMatcher.IsExcluded("/work/obj/file.txt", patterns));
		Assert.IsFalse(PathExclusionMatcher.IsExcluded("/work/src/file.txt", patterns));
	}

	[TestMethod]
	public void IsExcluded_BlankPatternsAreIgnored() =>
		Assert.IsFalse(PathExclusionMatcher.IsExcluded("/work/src/file.txt", ["", "   "]));

	[TestMethod]
	public void IsExcluded_NullArguments_Throw()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => PathExclusionMatcher.IsExcluded(null!, []));
		Assert.ThrowsExactly<ArgumentNullException>(() => PathExclusionMatcher.IsExcluded("/work/file.txt", null!));
	}
}
