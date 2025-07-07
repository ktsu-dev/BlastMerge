// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using ktsu.FileSystemProvider;

/// <summary>
/// Computes hashes for files to identify unique versions
/// </summary>
/// <param name="fileSystemProvider">File system provider for file operations</param>
public class FileHasher(IFileSystemProvider fileSystemProvider)
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

		using Stream fileStream = fileSystemProvider.Current.File.OpenRead(filePath);
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
	/// Computes an FNV-1a hash for a file asynchronously
	/// </summary>
	/// <param name="filePath">Path to the file</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The FNV-1a hash as a hex string</returns>
	public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
	{
		ulong hash = FNV_OFFSET_BASIS_64;

		using Stream fileStream = fileSystemProvider.Current.File.OpenRead(filePath);
		byte[] buffer = new byte[4096];
		int bytesRead;

		while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
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
	/// Computes FNV-1a hashes for multiple files in parallel
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
