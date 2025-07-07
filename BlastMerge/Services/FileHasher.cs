// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using ktsu.FileSystemProvider;

/// <summary>
/// Provides file hashing functionality with support for dependency injection.
/// </summary>
/// <param name="fileSystemProvider">File system provider for dependency injection</param>
public class FileHasher(IFileSystemProvider fileSystemProvider)
{
	private readonly IFileSystem _fileSystem = fileSystemProvider.Current;
	/// <summary>
	/// Computes a hash for the specified file.
	/// </summary>
	/// <param name="filePath">The path to the file to hash.</param>
	/// <returns>A hexadecimal string representation of the file hash.</returns>
	public string ComputeFileHash(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!_fileSystem.File.Exists(filePath))
		{
			throw new FileNotFoundException($"File not found: {filePath}");
		}

		using FileSystemStream stream = _fileSystem.File.OpenRead(filePath);
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(stream);

		// Convert to hexadecimal string
		string hash = Convert.ToHexString(hashBytes);

		// For compatibility with existing code, return as lowercase with specific format
		return hash.ToLowerInvariant();
	}

	/// <summary>
	/// Asynchronously computes a hash for the specified file.
	/// </summary>
	/// <param name="filePath">The path to the file to hash.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a hexadecimal string representation of the file hash.</returns>
	public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!_fileSystem.File.Exists(filePath))
		{
			throw new FileNotFoundException($"File not found: {filePath}");
		}

		using FileSystemStream stream = _fileSystem.File.OpenRead(filePath);
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

		// Convert to hexadecimal string
		string hash = Convert.ToHexString(hashBytes);

		// For compatibility with existing code, return as lowercase with specific format
		return hash.ToLowerInvariant();
	}

	/// <summary>
	/// Computes a hash for the specified file content.
	/// </summary>
	/// <param name="content">The content to hash.</param>
	/// <returns>A hexadecimal string representation of the content hash.</returns>
	public static string ComputeContentHash(string content)
	{
		ArgumentNullException.ThrowIfNull(content);
		byte[] contentBytes = Encoding.UTF8.GetBytes(content);
		byte[] hashBytes = SHA256.HashData(contentBytes);

		// Convert to hexadecimal string
		string hash = Convert.ToHexString(hashBytes);

		// For compatibility with existing code, return as lowercase with specific format
		return hash.ToLowerInvariant();
	}

	/// <summary>
	/// Computes hashes for multiple files in parallel
	/// </summary>
	/// <param name="filePaths">Collection of file paths to hash</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations (default: processor count)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping file paths to their hashes</returns>
	public async Task<Dictionary<string, string>> ComputeFileHashesAsync(
		IEnumerable<string> filePaths,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
		List<Task<(string filePath, string hash)>> tasks = [];

		foreach (string filePath in filePaths)
		{
			tasks.Add(ComputeHashWithSemaphore(filePath, semaphore, cancellationToken));
		}

		(string filePath, string hash)[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

		return results.ToDictionary(r => r.filePath, r => r.hash);
	}

	/// <summary>
	/// Helper method to compute hash with semaphore throttling
	/// </summary>
	private async Task<(string filePath, string hash)> ComputeHashWithSemaphore(
		string filePath,
		SemaphoreSlim semaphore,
		CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			string hash = await ComputeFileHashAsync(filePath, cancellationToken).ConfigureAwait(false);
			return (filePath, hash);
		}
		finally
		{
			semaphore.Release();
		}
	}

	/// <summary>
	/// Computes an FNV-1a hash for string content asynchronously
	/// </summary>
	/// <param name="content">String content to hash</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public static Task<string> ComputeContentHashAsync(string content)
	{
		ArgumentNullException.ThrowIfNull(content);

		// For string content hashing, we can run it on a background thread
		// since it's CPU-bound rather than I/O-bound
		return Task.Run(() => ComputeContentHash(content));
	}
}
