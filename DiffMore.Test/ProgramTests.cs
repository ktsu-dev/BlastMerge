// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.IO;
using ktsu.DiffMore.ConsoleApp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ProgramTests : IDisposable
{
	private readonly string _testDirectory;
	private readonly string _subDir1;
	private readonly string _subDir2;

	private readonly StringWriter _consoleOutput;
	private readonly TextWriter _originalConsoleOut;

	public ProgramTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_CLI");
		_subDir1 = Path.Combine(_testDirectory, "Dir1");
		_subDir2 = Path.Combine(_testDirectory, "Dir2");

		// Set up console output capture
		_consoleOutput = new StringWriter();
		_originalConsoleOut = Console.Out;
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directory structure
		Directory.CreateDirectory(_testDirectory);
		Directory.CreateDirectory(_subDir1);
		Directory.CreateDirectory(_subDir2);

		// Create test files with the same name but different content
		File.WriteAllLines(Path.Combine(_subDir1, "test.txt"),
		[
			"Line 1",
			"Line 2",
			"Line 3"
		]);

		File.WriteAllLines(Path.Combine(_subDir2, "test.txt"),
		[
			"Line 1",
			"Line 2 modified",
			"Line 3"
		]);

		// Redirect console output for testing
		Console.SetOut(_consoleOutput);
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Restore original console output
		Console.SetOut(_originalConsoleOut);
		_consoleOutput.Dispose();

		// Clean up test directory
		if (Directory.Exists(_testDirectory))
		{
			try
			{
				Directory.Delete(_testDirectory, true);
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
	}

	[TestMethod]
	public void Main_WithValidArgs_FindsFilesAndGroups()
	{
		// Arrange
		var args = new[] { _testDirectory, "test.txt" };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("Found 2 files"), "Should find 2 test.txt files");
		Assert.IsTrue(output.Contains("Found 2 unique versions"), "Should find 2 unique versions");
	}

	[TestMethod]
	public void Main_WithNoFiles_PrintsNoFilesFound()
	{
		// Arrange
		var args = new[] { _testDirectory, "nonexistent.txt" };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("No files with name 'nonexistent.txt' found"),
			"Should indicate that no files were found");
	}

	[TestMethod]
	public void Main_WithNonExistentDirectory_PrintsError()
	{
		// Arrange
		var nonExistentDir = Path.Combine(_testDirectory, "NonExistentDir");
		var args = new[] { nonExistentDir, "test.txt" };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains($"Error: Directory '{nonExistentDir}' does not exist"),
			"Should report that directory doesn't exist");
	}

	[TestMethod]
	public void Main_WithInsufficientArgs_PrintsUsage()
	{
		// Arrange
		var args = new[] { _testDirectory };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("Usage"), "Should print usage information");
	}

	[TestMethod]
	public void Main_WithNullArgs_PrintsUsage()
	{
		// Act
		Program.Main([]);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("Usage"), "Should print usage information");
	}

	[TestMethod]
	public void Main_WithIdenticalFiles_ReportsAllIdentical()
	{
		// Arrange
		File.WriteAllText(Path.Combine(_subDir2, "test.txt"), File.ReadAllText(Path.Combine(_subDir1, "test.txt")));
		var args = new[] { _testDirectory, "test.txt" };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("All files are identical"),
			"Should report that all files are identical");
		Assert.IsTrue(output.Contains("Found 1 unique version"),
			"Should find only 1 unique version");
	}

	[TestMethod]
	public void Main_WithDifferentFiles_ShowsDiff()
	{
		// Arrange
		var args = new[] { _testDirectory, "test.txt" };

		// Act
		Program.Main(args);
		var output = _consoleOutput.ToString();

		// Assert
		Assert.IsTrue(output.Contains("Diff between Version 1 and Version 2"),
			"Should show diff between versions");
		Assert.IsTrue(output.Contains("Line 2") && output.Contains("Line 2 modified"),
			"Diff should contain the modified line");
	}

	public void Dispose()
	{
		_consoleOutput?.Dispose();
		GC.SuppressFinalize(this);
	}
}
