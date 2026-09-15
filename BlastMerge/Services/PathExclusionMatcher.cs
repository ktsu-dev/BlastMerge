// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.TextFilter;

/// <summary>
/// Decides whether a path is excluded by a collection of user-supplied exclusion patterns.
/// </summary>
/// <remarks>
/// <para>
/// Glob matching is delegated to <see cref="TextFilter"/>, which parses patterns with DotNet.Glob.
/// The shapes BlastMerge has always advertised (<c>*/bin/*</c>, <c>*node_modules*</c>, <c>temp*</c>,
/// <c>*.txt</c>) do not mean the same thing under a real glob parser, because a single <c>*</c> does
/// not cross a directory separator. They are therefore translated into equivalent globs before
/// matching rather than passed through, so saved batch configurations keep working.
/// </para>
/// <para>
/// Matching is case-insensitive, as it has always been here. DotNet.Glob is case-sensitive and
/// exposes no option to change that, so both the path and the pattern are lowercased first. The one
/// consequence is that an explicit character class is lowercased too — <c>[A-Z]</c> behaves as
/// <c>[a-z]</c> — which matches the case-insensitive intent anyway.
/// </para>
/// </remarks>
internal static class PathExclusionMatcher
{
	/// <summary>
	/// Determines whether a path is excluded by any of the given patterns.
	/// </summary>
	/// <param name="normalizedPath">The path to test, rooted and using <c>/</c> as its separator.</param>
	/// <param name="exclusionPatterns">The exclusion patterns to match against. Blank patterns are ignored.</param>
	/// <returns><see langword="true"/> if any pattern matches the path; otherwise <see langword="false"/>.</returns>
	public static bool IsExcluded(string normalizedPath, IReadOnlyCollection<string> exclusionPatterns)
	{
		Ensure.NotNull(normalizedPath);
		Ensure.NotNull(exclusionPatterns);

		return exclusionPatterns.Count != 0
			&& exclusionPatterns.Any(pattern => Matches(normalizedPath, pattern));
	}

	/// <summary>
	/// Determines whether a path matches a single exclusion pattern.
	/// </summary>
	/// <param name="normalizedPath">The path to test, rooted and using <c>/</c> as its separator.</param>
	/// <param name="pattern">The exclusion pattern. Backslashes are treated as separators.</param>
	/// <returns><see langword="true"/> if the pattern matches the path; otherwise <see langword="false"/>.</returns>
	public static bool Matches(string normalizedPath, string pattern)
	{
		Ensure.NotNull(normalizedPath);

		if (string.IsNullOrWhiteSpace(pattern))
		{
			return false;
		}

		string normalizedPattern = pattern.Replace('\\', '/');

		// A pattern with no wildcard has never been a glob here: it excludes any path containing it.
		if (!normalizedPattern.Contains('*') && !normalizedPattern.Contains('?'))
		{
			return normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase);
		}

		string loweredPath = normalizedPath.ToLowerInvariant();

		return ToGlobs(normalizedPattern.ToLowerInvariant())
			.Any(glob => TextFilter.IsMatch(loweredPath, glob, TextFilterType.Glob, TextFilterMatchOptions.ByWholeString));
	}

	/// <summary>
	/// Translates an exclusion pattern into the globs that reproduce its established meaning.
	/// </summary>
	/// <param name="pattern">A lowercased pattern that contains at least one wildcard.</param>
	/// <returns>One or more globs; the path is excluded if any of them matches.</returns>
	private static IEnumerable<string> ToGlobs(string pattern)
	{
		// "*/bin/*" has always meant "has a bin directory component at any depth", which a real
		// glob spells "**/bin/**" -- a single "*" would pin bin to the second component.
		if (pattern.StartsWith("*/", StringComparison.Ordinal)
			&& pattern.EndsWith("/*", StringComparison.Ordinal)
			&& pattern.Length > 4)
		{
			return [$"**/{pattern[2..^2]}/**"];
		}

		// A pattern with no separator describes a single path component rather than a whole path:
		// "temp*" excludes any component starting with temp, "*.txt" any component ending in .txt.
		// Both forms below are needed because the matching component can be the last one (a file, or
		// the directory itself) or an ancestor directory.
		//
		// This is how "*x*" and "x*" always behaved. Other separator-less shapes -- "a?.txt", say --
		// used to fall through to a regex anchored to the whole path, so they could never match and
		// silently excluded nothing. They now match a component, like their siblings.
		if (!pattern.Contains('/'))
		{
			return [$"**/{pattern}", $"**/{pattern}/**"];
		}

		// Anything else is already a path-shaped pattern, anchored to the whole path.
		return [pattern];
	}
}
