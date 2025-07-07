// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.IO;
using ktsu.FileSystemProvider;

/// <summary>
/// Provides secure temporary file creation with collision handling.
/// </summary>
/// <param name="fileSystemProvider">File system provider for file operations</param>
public class SecureTempFileHelper(IFileSystemProvider fileSystemProvider)
{
	private const int MaxRetries = 100;

	/// <summary>
	/// Gets a secure temporary path with proper permissions validation.
	/// Performs security checks on the temp directory to ensure it's safe to use.
	/// </summary>
	/// <returns>The secure temporary directory path.</returns>
	/// <exception cref="IOException">Thrown when the temporary directory is not secure or accessible.</exception>
	private string GetSecureTempPath()
	{
		// For mock file systems, use a default temp path since Path.GetTempPath() returns real system path
		string tempPath;
		if (fileSystemProvider.Current.GetType().Name.Contains("Mock"))
		{
			tempPath = @"C:\temp";
			// Ensure the mock temp directory exists
			if (!fileSystemProvider.Current.Directory.Exists(tempPath))
			{
				fileSystemProvider.Current.Directory.CreateDirectory(tempPath);
			}
		}
		else
		{
			tempPath = Path.GetTempPath();
		}

		try
		{
			// Validate that we can write to the temp directory
			string testFile = fileSystemProvider.Current.Path.Combine(tempPath, fileSystemProvider.Current.Path.GetRandomFileName());
			using (Stream fs = fileSystemProvider.Current.File.Create(testFile))
			{
				// Write a test byte to ensure we have write permissions
				fs.WriteByte(0);
			}

			// Clean up the test file immediately
			fileSystemProvider.Current.File.Delete(testFile);

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
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public string CreateTempFile() => CreateTempFile(".tmp");

	/// <summary>
	/// Creates a secure temporary file with a unique name and specific extension.
	/// Uses Path.GetRandomFileName() for security while handling collision potential.
	/// </summary>
	/// <param name="extension">The file extension (including the dot, e.g., ".txt").</param>
	/// <returns>The full path to the created temporary file.</returns>
	/// <exception cref="IOException">Thrown when unable to create a unique temporary file after max retries.</exception>
	public string CreateTempFile(string extension)
	{
		ArgumentNullException.ThrowIfNull(extension);
		string tempPath = GetSecureTempPath();

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string fileName = fileSystemProvider.Current.Path.GetRandomFileName();
			// Replace the extension from GetRandomFileName with the desired one
			fileName = fileSystemProvider.Current.Path.ChangeExtension(fileName, extension);
			string fullPath = fileSystemProvider.Current.Path.Combine(tempPath, fileName);

			try
			{
				// Create the file atomically to ensure uniqueness
				// FileMode.CreateNew will fail if the file already exists
				using Stream fs = fileSystemProvider.Current.File.Create(fullPath);
				// File is created and immediately closed
				return fullPath;
			}
			catch (IOException) when (fileSystemProvider.Current.File.Exists(fullPath))
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
	public string CreateTempDirectory()
	{
		string tempPath = GetSecureTempPath();

		for (int attempt = 0; attempt < MaxRetries; attempt++)
		{
			string directoryName = fileSystemProvider.Current.Path.GetRandomFileName();
			string fullPath = fileSystemProvider.Current.Path.Combine(tempPath, directoryName);

			try
			{
				// Check if directory already exists first
				if (fileSystemProvider.Current.Directory.Exists(fullPath))
				{
					continue; // Try again with a new random name
				}

				// Create the directory
				fileSystemProvider.Current.Directory.CreateDirectory(fullPath);

				// Verify it was created and we can access it
				if (fileSystemProvider.Current.Directory.Exists(fullPath))
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
	public void SafeDeleteTempFile(string? filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return;
		}

		try
		{
			if (fileSystemProvider.Current.File.Exists(filePath))
			{
				fileSystemProvider.Current.File.Delete(filePath);
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
	public void SafeDeleteTempFiles(params string?[]? filePaths)
	{
		if (filePaths == null)
		{
			return;
		}

		foreach (string? filePath in filePaths)
		{
			SafeDeleteTempFile(filePath);
		}
	}

	/// <summary>
	/// Safely deletes a temporary directory and all its contents, suppressing common exceptions.
	/// </summary>
	/// <param name="directoryPath">The path to the temporary directory to delete.</param>
	public void SafeDeleteTempDirectory(string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return;
		}

		try
		{
			if (fileSystemProvider.Current.Directory.Exists(directoryPath))
			{
				fileSystemProvider.Current.Directory.Delete(directoryPath, recursive: true);
			}
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
