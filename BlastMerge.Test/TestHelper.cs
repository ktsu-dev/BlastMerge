// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;

/// <summary>
/// Helper class for unit tests
/// </summary>
internal static class TestHelper
{
	/// <summary>
	/// Creates a temporary file with the specified content and returns its path
	/// </summary>
	/// <param name="content">Content to write to the file</param>
	/// <param name="extension">Optional file extension (default is .txt)</param>
	/// <returns>The path to the created temporary file</returns>
	public static string CreateTempFile(string content, string extension = ".txt")
	{
		string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
		File.WriteAllText(tempFile, content);
		return tempFile;
	}

	/// <summary>
	/// Creates a temporary file with the specified lines and returns its path
	/// </summary>
	/// <param name="lines">Lines to write to the file</param>
	/// <param name="extension">Optional file extension (default is .txt)</param>
	/// <returns>The path to the created temporary file</returns>
	public static string CreateTempFile(string[] lines, string extension = ".txt")
	{
		string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
		File.WriteAllLines(tempFile, lines);
		return tempFile;
	}

	/// <summary>
	/// Creates a temporary directory and returns its path
	/// </summary>
	/// <returns>The path to the created temporary directory</returns>
	public static string CreateTempDirectory()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
		return tempDir;
	}

	/// <summary>
	/// Safely deletes a file if it exists
	/// </summary>
	/// <param name="filePath">Path to the file to delete</param>
	public static void SafeDeleteFile(string filePath)
	{
		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}
		catch (IOException)
		{
			// Ignore IO exceptions during cleanup
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access exceptions during cleanup
		}
	}

	/// <summary>
	/// Safely deletes a directory and all its contents if it exists
	/// </summary>
	/// <param name="directoryPath">Path to the directory to delete</param>
	public static void SafeDeleteDirectory(string directoryPath)
	{
		try
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
		catch (IOException)
		{
			// Ignore IO exceptions during cleanup
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access exceptions during cleanup
		}
	}

	/// <summary>
	/// Safely creates a directory if it doesn't exist
	/// </summary>
	/// <param name="directoryPath">Path to the directory to create</param>
	public static void SafeCreateDirectory(string directoryPath)
	{
		try
		{
			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}
		}
		catch (IOException)
		{
			// Ignore IO exceptions during creation
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access exceptions during creation
		}
	}

	/// <summary>
	/// Gets the absolute path of a test file in the TestFiles directory
	/// </summary>
	/// <param name="relativeFilePath">Relative path to the test file</param>
	/// <returns>The absolute path to the test file</returns>
	public static string GetTestFilePath(string relativeFilePath)
	{
		// Find the solution directory
		DirectoryInfo? currentDir = new(Directory.GetCurrentDirectory());
		while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "BlastMerge.sln")))
		{
			currentDir = currentDir.Parent;
		}

		if (currentDir == null)
		{
			throw new DirectoryNotFoundException("Could not find solution directory");
		}

		// Return the path to the test file
		return Path.Combine(currentDir.FullName, "BlastMerge.CLI", "TestFiles", relativeFilePath);
	}
}
