// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;

/// <summary>
/// Builds rooted paths that are valid on the platform the tests are running on.
/// </summary>
/// <remarks>
/// Hard-coding <c>C:\</c> roots makes tests pass on Windows and fail everywhere else:
/// on Unix a backslash is an ordinary filename character, so <c>C:\dir\file.txt</c> is a
/// single-component relative path rather than a rooted one. Tests that need a rooted path
/// build it here instead.
/// </remarks>
internal static class TestPaths
{
	/// <summary>
	/// Gets the root of a rooted path on the current platform.
	/// </summary>
	public static string Root { get; } = OperatingSystem.IsWindows() ? @"C:\" : "/";

	/// <summary>
	/// Combines the given components into a rooted, platform-native path.
	/// </summary>
	/// <param name="components">The path components to append to the platform root.</param>
	/// <returns>A rooted path using the platform's directory separator.</returns>
	public static string Rooted(params string[] components) => Path.Combine([Root, .. components]);
}
