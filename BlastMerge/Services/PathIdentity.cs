// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

/// <summary>
/// Tells whether two path strings name the same file, however each is spelled.
/// </summary>
internal static class PathIdentity
{
	/// <summary>
	/// Compares full paths as the platform's default file system does: ignoring case on Windows and macOS,
	/// and ordinally elsewhere.
	/// </summary>
	internal static StringComparer Comparer { get; } =
		OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	/// <summary>
	/// Removes paths that name a file already in the sequence, keeping the first spelling of each.
	/// </summary>
	/// <remarks>
	/// Search paths that overlap but are written differently (relative and absolute, <c>/x/./a</c>,
	/// <c>/x/a/../a</c>) find the same file under different strings, which an ordinal
	/// <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/> keeps as two copies.
	/// </remarks>
	/// <param name="paths">The paths to remove duplicates from.</param>
	/// <param name="fileSystem">The file system whose current directory resolves relative paths.</param>
	/// <returns>The paths, each file once, in their original order and spelling.</returns>
	internal static IEnumerable<string> DistinctFiles(IEnumerable<string> paths, IFileSystem fileSystem) =>
		paths.DistinctBy(fileSystem.Path.GetFullPath, Comparer);
}
