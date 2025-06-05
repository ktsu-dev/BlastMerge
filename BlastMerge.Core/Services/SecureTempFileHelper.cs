// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.IO;

/// <summary>
/// Provides secure temporary file creation with collision handling.
/// </summary>
public static class SecureTempFileHelper
{
	private const int MaxRetries = 100;

	/// <summary>
	/// Gets a secure temporary path with proper permissions validation.
	/// Performs security checks on the temp directory to ensure it's safe to use.
	/// </summary>
	/// <returns>The secure temporary directory path.</returns>
	/// <exception cref="IOException">Thrown when the temporary directory is not secure or accessible.</exception>
	private static string GetSecureTempPath()
	{
		string tempPath = Path.GetTempPath();

		try
		{
			// Validate that we can write to the temp directory
			string testFile = Path.Combine(tempPath, Path.GetRandomFileName());
			using (FileStream fs = new(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				// Write a test byte to ensure we have write permissions
				fs.WriteByte(0);
			}

			// Clean up the test file immediately
			File.Delete(testFile);

			return tempPath;
		}
		catch (UnauthorizedAccessException ex)
		{
			throw new IOException($"Temporary directory '{tempPath}' is not writable due to insufficient permissions.", ex);
		}
		catch (IOException ex)
		{
			throw new IOException($"Temporary directory '{tempPath}' is not accessible for secure file creation.", ex);
		}
	}

	/// <summary>
	/// Creates a secure temporary file with a unique name.
	/// Uses Path.GetRandomFileName() for security while handling collision potential.
	/// </summary>
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public static string CreateTempFile()
	{
		string tempPath = GetSecureTempPath();

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string fileName = Path.GetRandomFileName();
			string fullPath = Path.Combine(tempPath, fileName);

			try
			{
				// Create the file atomically to ensure uniqueness
				// FileMode.CreateNew will fail if the file already exists
				using FileStream fileStream = new(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
				// File is created and immediately closed
				return fullPath;
			}
			catch (IOException) when (File.Exists(fullPath))
			{
				// File already exists, try again with a new random name
				continue;
			}
		}

		throw new IOException($"Unable to create a unique temporary file after {MaxRetries} attempts.");
	}

	/// <summary>
	/// Creates a secure temporary file with a unique name and specific extension.
	/// Uses Path.GetRandomFileName() for security while handling collision potential.
	/// </summary>
	/// <param name="extension">The file extension (including the dot, e.g., ".txt").</param>
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public static string CreateTempFile(string extension)
	{
		ArgumentNullException.ThrowIfNull(extension);

		string tempPath = GetSecureTempPath();

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string fileName = Path.GetRandomFileName();
			// Replace the extension from GetRandomFileName with the desired one
			fileName = Path.ChangeExtension(fileName, extension);
			string fullPath = Path.Combine(tempPath, fileName);

			try
			{
				// Create the file atomically to ensure uniqueness
				// FileMode.CreateNew will fail if the file already exists
				using FileStream fileStream = new(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
				// File is created and immediately closed
				return fullPath;
			}
			catch (IOException) when (File.Exists(fullPath))
			{
				// File already exists, try again with a new random name
				continue;
			}
		}

		throw new IOException($"Unable to create a unique temporary file with extension '{extension}' after {MaxRetries} attempts.");
	}

	/// <summary>
	/// Creates a secure temporary directory with a unique name.
	/// </summary>
	/// <returns>The full path to the created temporary directory.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary directory after max retries.</exception>
	public static string CreateTempDirectory()
	{
		string tempPath = GetSecureTempPath();

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string directoryName = Path.GetRandomFileName();
			string fullPath = Path.Combine(tempPath, directoryName);

			try
			{
				// Create the directory atomically to ensure uniqueness
				// DirectoryInfo.Create() will not fail if the directory already exists,
				// so we need to check first
				if (!Directory.Exists(fullPath))
				{
					Directory.CreateDirectory(fullPath);
					return fullPath;
				}
			}
			catch (IOException)
			{
				// Directory creation failed, try again with a new random name
				continue;
			}
		}

		throw new IOException($"Unable to create a unique temporary directory after {MaxRetries} attempts.");
	}

	/// <summary>
	/// Safely deletes a temporary file, suppressing common exceptions.
	/// </summary>
	/// <param name="filePath">The path to the temporary file to delete.</param>
	public static void SafeDeleteTempFile(string? filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}

		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}
		catch (IOException)
		{
			// Ignore IO exceptions when cleaning up temp files
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access exceptions when cleaning up temp files
		}
		catch (ArgumentException)
		{
			// Ignore argument exceptions for invalid paths
		}
	}

	/// <summary>
	/// Safely deletes multiple temporary files.
	/// </summary>
	/// <param name="filePaths">The paths to the temporary files to delete.</param>
	public static void SafeDeleteTempFiles(params string?[] filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		foreach (string? filePath in filePaths)
		{
			SafeDeleteTempFile(filePath);
		}
	}

	/// <summary>
	/// Safely deletes a temporary directory and all its contents, suppressing common exceptions.
	/// </summary>
	/// <param name="directoryPath">The path to the temporary directory to delete.</param>
	public static void SafeDeleteTempDirectory(string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}

		try
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, recursive: true);
			}
		}
		catch (DirectoryNotFoundException)
		{
			// Directory doesn't exist, which is fine for cleanup
		}
		catch (IOException)
		{
			// Ignore IO exceptions when cleaning up temp directories
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access exceptions when cleaning up temp directories
		}
		catch (ArgumentException)
		{
			// Ignore argument exceptions for invalid paths
		}
	}
}
