// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test.Adapters;

using System;
using System.IO.Abstractions;

/// <summary>
/// Adapter for FileHasher that works with a mock filesystem
/// </summary>
/// <remarks>
/// Initializes a new instance of the FileHasherAdapter class
/// </remarks>
/// <param name="fileSystem">The file system to use</param>
public class FileHasherAdapter(IFileSystem fileSystem)
{
	// FNV-1a constants (64-bit version)
	private const ulong FNV_PRIME_64 = 1099511628211;
	private const ulong FNV_OFFSET_BASIS_64 = 14695981039346656037;

	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

	/// <summary>
	/// Computes an FNV-1a hash for a file
	/// </summary>
	/// <param name="filePath">Path to the file</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public string ComputeFileHash(string filePath)
	{
		var hash = FNV_OFFSET_BASIS_64;

		using var fileStream = _fileSystem.File.OpenRead(filePath);
		var buffer = new byte[4096];
		int bytesRead;

		while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
		{
			for (var i = 0; i < bytesRead; i++)
			{
				hash ^= buffer[i];
				hash *= FNV_PRIME_64;
			}
		}

		return hash.ToString("x16");
	}
}
