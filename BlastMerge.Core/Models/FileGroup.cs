// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Represents a group of identical files by hash
/// </summary>
/// <param name="Hash"> Gets the hash that identifies this group </param>
public record FileGroup(string Hash = "")
{
	private readonly Collection<string> filePaths = [];

	/// <summary>
	/// Gets the list of file paths in this group
	/// </summary>
	public IReadOnlyCollection<string> FilePaths => filePaths.AsReadOnly();

	/// <summary>
	/// Initializes a new instance of the FileGroup class with the specified file paths
	/// </summary>
	/// <param name="filePaths">The file paths to include in this group</param>
	public FileGroup(IEnumerable<string> filePaths) : this("")
	{
		ArgumentNullException.ThrowIfNull(filePaths);
		foreach (string filePath in filePaths)
		{
			this.filePaths.Add(filePath);
		}
	}

	/// <summary>
	/// Adds a file path to this group
	/// </summary>
	/// <param name="filePath">The file path to add</param>
	public void AddFilePath(string filePath) => filePaths.Add(filePath);
}

