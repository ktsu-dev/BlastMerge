// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

/// <summary>
/// Computes hashes for files to identify unique versions
/// </summary>
public static class FileHasher
{
	// FNV-1a constants (64-bit version)
	private const ulong FNV_PRIME_64 = 1099511628211;
	private const ulong FNV_OFFSET_BASIS_64 = 14695981039346656037;

	/// <summary>
	/// Computes an FNV-1a hash for a file
	/// </summary>
	/// <param name="filePath">Path to the file</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public static string ComputeFileHash(string filePath)
	{
		ulong hash = FNV_OFFSET_BASIS_64;

		using FileStream fileStream = File.OpenRead(filePath);
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

	/// <summary>
	/// Computes an FNV-1a hash for string content
	/// </summary>
	/// <param name="content">String content to hash</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public static string ComputeContentHash(string content)
	{
		ArgumentNullException.ThrowIfNull(content);

		ulong hash = FNV_OFFSET_BASIS_64;
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

		foreach (byte b in bytes)
		{
			hash ^= b;
			hash *= FNV_PRIME_64;
		}

		return hash.ToString("x16");
	}
}
