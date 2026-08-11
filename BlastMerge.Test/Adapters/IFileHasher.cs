// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test.Adapters;

/// <summary>
/// Interface for file hashing operations to support mocking.
/// </summary>
public interface IFileHasher
{
	/// <summary>
	/// Computes a hash for the specified file.
	/// </summary>
	/// <param name="filePath">The path to the file to hash.</param>
	/// <returns>The computed hash as a string.</returns>
	public string ComputeFileHash(string filePath);
}
