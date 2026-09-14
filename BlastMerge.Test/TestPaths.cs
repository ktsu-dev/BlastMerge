// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.Linq;

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
	public static string Rooted(params string[] components)
	{
		ArgumentNullException.ThrowIfNull(components);

		// Path.Combine discards every argument before a rooted one, so a component that is
		// itself rooted would silently drop the platform root this method exists to apply.
		// Reduce each component to a relative segment first.
		return Path.Combine([Root, .. components.Select(MakeRelative)]);
	}

	/// <summary>
	/// Strips any root prefix and leading separators from a path component.
	/// </summary>
	/// <param name="component">The component to make relative.</param>
	/// <returns>The component with no root prefix or leading directory separator.</returns>
	private static string MakeRelative(string component)
	{
		ArgumentNullException.ThrowIfNull(component);

		string root = Path.GetPathRoot(component) ?? string.Empty;
		return component[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}
}
