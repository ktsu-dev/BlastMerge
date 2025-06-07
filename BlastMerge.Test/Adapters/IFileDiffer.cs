// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System.Collections.ObjectModel;

/// <summary>
/// Interface for file diffing operations to support mocking.
/// </summary>
public interface IFileDiffer
{
	/// <summary>
	/// Finds differences between two files.
	/// </summary>
	/// <param name="file1Path">The path to the first file.</param>
	/// <param name="file2Path">The path to the second file.</param>
	/// <returns>A read-only collection of difference strings.</returns>
	public ReadOnlyCollection<string> FindDifferences(string file1Path, string file2Path);

	/// <summary>
	/// Generates a Git-style diff between two files.
	/// </summary>
	/// <param name="file1Path">The path to the first file.</param>
	/// <param name="file2Path">The path to the second file.</param>
	/// <returns>A Git-style diff as a string.</returns>
	public string GenerateGitStyleDiff(string file1Path, string file2Path);
}
