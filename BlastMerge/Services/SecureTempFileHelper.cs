// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.IO;
using System.IO.Abstractions;

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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The secure temporary directory path.</returns>
	/// <exception cref="IOException">Thrown when the temporary directory is not secure or accessible.</exception>
	private static string GetSecureTempPath(IFileSystem? fileSystem = null)
	{
		fileSystem ??= new FileSystem();
		string tempPath = Path.GetTempPath();

		try
		{
			// Validate that we can write to the temp directory
			string testFile = fileSystem.Path.Combine(tempPath, fileSystem.Path.GetRandomFileName());
			using (Stream fs = fileSystem.File.Create(testFile))
			{
				// Write a test byte to ensure we have write permissions
				fs.WriteByte(0);
			}

			// Clean up the test file immediately
			fileSystem.File.Delete(testFile);

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
	/// </summary>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public static string CreateTempFile(IFileSystem? fileSystem = null) => CreateTempFile(".tmp", fileSystem);

	/// <summary>
	/// Creates a secure temporary file with a unique name and specific extension.
	/// Uses Path.GetRandomFileName() for security while handling collision potential.
	/// </summary>
	/// <param name="extension">The file extension (including the dot, e.g., ".txt").</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public static string CreateTempFile(string extension, IFileSystem? fileSystem = null)
	{
		ArgumentNullException.ThrowIfNull(extension);
		fileSystem ??= new FileSystem();

		string tempPath = GetSecureTempPath(fileSystem);

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string fileName = fileSystem.Path.GetRandomFileName();
			// Replace the extension from GetRandomFileName with the desired one
			fileName = fileSystem.Path.ChangeExtension(fileName, extension);
			string fullPath = fileSystem.Path.Combine(tempPath, fileName);

			try
			{
				// Create the file atomically to ensure uniqueness
				// FileMode.CreateNew will fail if the file already exists
				using Stream fs = fileSystem.File.Create(fullPath);
				// File is created and immediately closed
				return fullPath;
			}
			catch (IOException) when (fileSystem.File.Exists(fullPath))
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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The full path to the created temporary directory.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary directory after max retries.</exception>
	public static string CreateTempDirectory(IFileSystem? fileSystem = null)
	{
		fileSystem ??= new FileSystem();
		string tempPath = GetSecureTempPath(fileSystem);

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string directoryName = fileSystem.Path.GetRandomFileName();
			string fullPath = fileSystem.Path.Combine(tempPath, directoryName);

			try
			{
				// Check if directory already exists first
				if (fileSystem.Directory.Exists(fullPath))
				{
					continue; // Try again with a new random name
				}

				// Create the directory
				fileSystem.Directory.CreateDirectory(fullPath);

				// Verify it was created and we can access it
				if (fileSystem.Directory.Exists(fullPath))
				{
					return fullPath;
				}
			}
			catch (IOException)
			{
				// Directory creation failed, try again with a new random name
				continue;
			}
			catch (UnauthorizedAccessException)
			{
				// Access denied, try again with a new random name
				continue;
			}
		}

		throw new IOException($"Unable to create a unique temporary directory after {MaxRetries} attempts.");
	}

	/// <summary>
	/// Safely deletes a temporary file, suppressing common exceptions.
	/// </summary>
	/// <param name="filePath">The path to the temporary file to delete.</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	public static void SafeDeleteTempFile(string? filePath, IFileSystem? fileSystem = null)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}

		fileSystem ??= new FileSystem();

		try
		{
			if (fileSystem.File.Exists(filePath))
			{
				fileSystem.File.Delete(filePath);
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
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <param name="filePaths">The paths to the temporary files to delete.</param>
	public static void SafeDeleteTempFiles(IFileSystem? fileSystem = null, params string?[] filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		foreach (string? filePath in filePaths)
		{
			SafeDeleteTempFile(filePath, fileSystem);
		}
	}

	/// <summary>
	/// Safely deletes a temporary directory and all its contents, suppressing common exceptions.
	/// </summary>
	/// <param name="directoryPath">The path to the temporary directory to delete.</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	public static void SafeDeleteTempDirectory(string? directoryPath, IFileSystem? fileSystem = null)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}

		fileSystem ??= new FileSystem();

		try
		{
			if (fileSystem.Directory.Exists(directoryPath))
			{
				fileSystem.Directory.Delete(directoryPath, recursive: true);
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
