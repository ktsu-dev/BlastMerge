// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

/// <summary>
/// Interface for centralized comparison operations service.
/// </summary>
public interface IComparisonOperationsService
{
	/// <summary>
	/// Handles comparing two directories with user input and display.
	/// </summary>
	public void HandleCompareTwoDirectories();

	/// <summary>
	/// Handles comparing two specific files with user input and display.
	/// </summary>
	public void HandleCompareTwoSpecificFiles();

	/// <summary>
	/// Compares two directories and displays results.
	/// </summary>
	/// <param name="dir1">First directory path.</param>
	/// <param name="dir2">Second directory path.</param>
	/// <param name="pattern">File search pattern.</param>
	/// <param name="recursive">Whether to search recursively.</param>
	public void CompareDirectories(string dir1, string dir2, string pattern, bool recursive);
}
