// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System.Collections.ObjectModel;

/// <summary>
/// Interface for file finding operations to support mocking.
/// </summary>
public interface IFileFinder
{
	/// <summary>
	/// Finds files matching the specified pattern in the given directory.
	/// </summary>
	/// <param name="directoryPath">The directory path to search.</param>
	/// <param name="searchPattern">The search pattern for files.</param>
	/// <returns>A read-only collection of file paths.</returns>
	public ReadOnlyCollection<string> FindFiles(string directoryPath, string searchPattern);
}
