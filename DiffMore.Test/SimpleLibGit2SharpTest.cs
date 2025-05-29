// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.DiffMore.Core;

/// <summary>
/// Simple test to verify LibGit2Sharp basic functionality
/// </summary>
[TestClass]
public class SimpleLibGit2SharpTest
{
	private string _testDirectory = null!;
	private string _file1 = null!;
	private string _file2 = null!;

	[TestInitialize]
	public void Setup()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"SimpleLibGit2SharpTest_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);

		_file1 = Path.Combine(_testDirectory, "file1.txt");
		_file2 = Path.Combine(_testDirectory, "file2.txt");

		File.WriteAllLines(_file1, ["Line 1", "Line 2", "Line 3"]);
		File.WriteAllLines(_file2, ["Line 1", "Modified Line 2", "Line 3"]);
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, recursive: true);
		}
	}

	[TestMethod]
	public void BasicDiffTest()
	{
		try
		{
			var diff = LibGit2SharpDiffer.GenerateGitStyleDiff(_file1, _file2);
			Console.WriteLine($"Diff result: {diff}");
			Assert.IsNotNull(diff, "Diff should not be null");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex}");
			throw;
		}
	}
}
