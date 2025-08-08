// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test.Adapters;

using System.IO.Abstractions;
using ktsu.FileSystemProvider;

/// <summary>
/// Adapter for FileHasher that works with a mock filesystem
/// </summary>
/// <remarks>
/// Initializes a new instance of the FileHasherAdapter class
/// </remarks>
/// <param name="fileSystemProvider">The file system to use</param>
public class FileHasherAdapter(IFileSystemProvider fileSystemProvider)
{
	// FNV-1a constants (64-bit version)
	private const ulong FNV_PRIME_64 = 1099511628211;
	private const ulong FNV_OFFSET_BASIS_64 = 14695981039346656037;

	/// <summary>
	/// Computes an FNV-1a hash for a file
	/// </summary>
	/// <param name="filePath">Path to the file</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public string ComputeFileHash(string filePath)
	{
		ulong hash = FNV_OFFSET_BASIS_64;

		using FileSystemStream fileStream = fileSystemProvider.Current.File.OpenRead(filePath);
		byte[] buffer = new byte[4096];
		int bytesRead;

		while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
		{
			for (int i = 0; i < bytesRead; i++)
			{
				hash ^= buffer[i];
				hash *= FNV_PRIME_64;
			}
		}

		return hash.ToString("x16");
	}
}
