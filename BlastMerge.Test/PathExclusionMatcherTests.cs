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

	// A relative pattern containing a separator used to be anchored to the start of the rooted path,
	// so the ".git/*" the batch prompt and README suggest excluded nothing.
	[TestMethod]
	[DataRow("/r/.git/config", ".git/*")]
	[DataRow("/r/.git/refs/heads/main", ".git/*")]
	[DataRow("/r/bin/x.dll", "bin/*")]
	[DataRow("/r/src/generated/a.cs", "src/generated/*")]
	[DataRow("/r/src/obj", "*/obj")]
	[DataRow("/r/src/obj/x.dll", "*/obj")]
	public void Matches_RelativePatternWithSeparator_MatchesAtAnyDepth(string path, string pattern)
	{
		Assert.IsTrue(PathExclusionMatcher.Matches(path, pattern));
	}

	// Shell-style "./bin/*" and gitignore-style "*/node_modules/" or "bin/" excluded nothing: the
	// tested path never contains "/./", and a trailing separator left an empty last component that
	// no file could match (ktsu-dev/BlastMerge#116).
	[TestMethod]
	[DataRow("/w/repo/bin/x.dll", "./bin/*")]
	[DataRow("/w/repo/bin/x.dll", "././bin/*")]
	[DataRow("/w/repo/bin/x.dll", "bin/")]
	[DataRow("/w/repo/bin/x.dll", "./bin/")]
	[DataRow("/w/repo/bin/x.dll", "*/bin/")]
	[DataRow("/w/repo/node_modules/a/b.js", "*/node_modules/")]
	[DataRow("/w/repo/node_modules/a/b.js", "./node_modules/*")]
	[DataRow("/w/repo/build-out/x.dll", "build*/")]
	[DataRow("/w/repo/bin/x.dll", ".\\bin\\*")]
	public void Matches_DotSlashOrTrailingSlashPattern_MatchesTheDirectory(string path, string pattern)
	{
		Assert.IsTrue(PathExclusionMatcher.Matches(path, pattern));
	}

	[TestMethod]
	public void Matches_TrailingSlashPattern_MatchesWholeComponents()
	{
		Assert.IsFalse(PathExclusionMatcher.Matches("/w/repo/binaries/x.dll", "*/bin/"),
			"A component merely starting with 'bin' is not a 'bin' component.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/w/repo/mybin/x.dll", "./bin/*"),
			"A component merely ending in 'bin' is not a 'bin' component.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/w/repo/src/x.cs", "*/node_modules/"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/w/repo/src/x.cs", "./"),
			"A pattern that is only './' names nothing, so it must not exclude every path.");
	}

	[TestMethod]
	public void Matches_RelativePatternWithSeparator_MatchesWholeComponents()
	{
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/mybin/x.dll", "bin/*"),
			"A component merely ending in 'bin' is not a 'bin' component.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/config", ".git/*"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/generated/a.cs", "src/generated/*"));
	}

	[TestMethod]
	public void Matches_AbsolutePattern_StaysAnchored()
	{
		Assert.IsFalse(PathExclusionMatcher.Matches("/other/work/src/a.tmp", "/work/src/*.tmp"),
			"An absolute pattern must not match the same tail under a different root.");
		Assert.IsTrue(PathExclusionMatcher.Matches("c:/work/src/a.tmp", "C:/work/src/*.tmp"));
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
	public void Matches_PatternWithoutWildcards_MatchesAWholeComponent()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/generated/file.txt", "generated"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/src/file.txt", "generated"));
	}

	[TestMethod]
	public void Matches_PatternWithoutWildcards_DoesNotMatchPartOfAName()
	{
		// ktsu-dev/BlastMerge#79: a bare "bin" or "obj" used to exclude any path containing those letters.
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/bin/x.dll", "bin"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/src/bin", "bin"), "A file named exactly bin is excluded.");
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/src/Combine.cs", "bin"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/cabinet/x.txt", "bin"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/src/ObjectPool.cs", "obj"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/src/Contest.cs", "test"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/digit.txt", "git"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/.git/config", ".git"));
	}

	[TestMethod]
	public void Matches_PatternWithoutWildcards_WithSeparator_MatchesARunOfComponents()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/src/generated/a.cs", "src/generated"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/src/generated/a.cs", "src/generated/"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/src/generated/a.cs", @"src\generated"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/src/generatedcode/a.cs", "src/generated"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/mysrc/generated/a.cs", "src/generated"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/generated/src/a.cs", "src/generated"));
	}

	[TestMethod]
	public void Matches_AbsolutePatternWithoutWildcards_IsAnchoredToTheRoot()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src/a.cs", "/work/src"));
		Assert.IsTrue(PathExclusionMatcher.Matches("/work/src", "/work/src"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/other/work/src/a.cs", "/work/src"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/work/srcs/a.cs", "/work/src"));
		Assert.IsTrue(PathExclusionMatcher.Matches("c:/work/src/a.cs", "C:/Work"));
	}

	[TestMethod]
	public void Matches_PatternWithoutWildcards_TreatsBracketsLiterally()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/pages/[id]/page.tsx", "[id]"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/pages/i/page.tsx", "[id]"));
	}

	// ktsu-dev/BlastMerge#149: brackets meant a literal name without a wildcard and a character
	// class with one, so "app/[id]/*" excluded app/d/ and "[Bb]in/" excluded nothing.
	[TestMethod]
	[DataRow("/r/app/[id]/page.tsx", "app/[id]")]
	[DataRow("/r/app/[id]/page.tsx", "app/[id]/*")]
	[DataRow("/r/app/[id]/page.tsx", "app/[id]/")]
	[DataRow("/r/app/[...slug]/page.tsx", "app/[...slug]/*")]
	[DataRow("/r/Bin/page.tsx", "[Bb]in/")]
	[DataRow("/r/Bin/page.tsx", "[Bb]in/*")]
	[DataRow("/r/bin/page.tsx", "[Bb]in")]
	[DataRow("/r/obj/x.dll", "[Oo]bj/")]
	[DataRow("/r/src/a.c", "*.[ch]")]
	public void Matches_Brackets_MeanTheSameWithOrWithoutAWildcard_Excluded(string path, string pattern) =>
		Assert.IsTrue(PathExclusionMatcher.Matches(path, pattern), $"'{pattern}' should exclude '{path}'.");

	[TestMethod]
	[DataRow("/r/app/d/page.tsx", "app/[id]")]
	[DataRow("/r/app/d/page.tsx", "app/[id]/*")]
	[DataRow("/r/app/i/page.tsx", "app/[id]/*")]
	[DataRow("/r/app/d/page.tsx", "app/[id]/")]
	[DataRow("/r/app/home/page.tsx", "app/[id]/*")]
	[DataRow("/r/app/[id]/page.tsx", "[Bb]in/")]
	[DataRow("/r/cabin/page.tsx", "[Bb]in/")]
	[DataRow("/r/src/a.cs", "*.[ch]")]
	public void Matches_Brackets_MeanTheSameWithOrWithoutAWildcard_Kept(string path, string pattern) =>
		Assert.IsFalse(PathExclusionMatcher.Matches(path, pattern), $"'{pattern}' should not exclude '{path}'.");

	[TestMethod]
	public void Matches_UnclosedBracket_IsLiteral()
	{
		Assert.IsTrue(PathExclusionMatcher.Matches("/r/a[b/x.txt", "a[b/*"));
		Assert.IsFalse(PathExclusionMatcher.Matches("/r/ab/x.txt", "a[b/*"));
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
