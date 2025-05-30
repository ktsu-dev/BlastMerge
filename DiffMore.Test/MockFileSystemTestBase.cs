// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base class for tests that use a mock file system
/// </summary>
public abstract class MockFileSystemTestBase
{
	/// <summary>
	/// The mock file system instance used for testing
	/// </summary>
	protected MockFileSystem MockFileSystem { get; private set; } = null!;

	/// <summary>
	/// The root directory for the mock file system
	/// </summary>
	protected string TestDirectory { get; private set; } = null!;

	/// <summary>
	/// Initializes the mock file system
	/// </summary>
	[TestInitialize]
	public virtual void SetUp()
	{
		TestDirectory = @"C:\mock-test-dir";
		MockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
		MockFileSystem.Directory.CreateDirectory(TestDirectory);

		InitializeFileSystem();
	}

	/// <summary>
	/// Override this method to set up your specific test files and directories
	/// </summary>
	protected virtual void InitializeFileSystem()
	{
		// To be overridden by derived classes
	}

	/// <summary>
	/// Creates a file in the mock filesystem
	/// </summary>
	/// <param name="relativePath">Path relative to the test directory</param>
	/// <param name="content">Content to write to the file</param>
	/// <returns>Full path to the created file</returns>
	protected string CreateFile(string relativePath, string content)
	{
		var fullPath = Path.Combine(TestDirectory, relativePath);
		var directory = Path.GetDirectoryName(fullPath);

		if (!string.IsNullOrEmpty(directory) && !MockFileSystem.Directory.Exists(directory))
		{
			MockFileSystem.Directory.CreateDirectory(directory);
		}

		MockFileSystem.File.WriteAllText(fullPath, content);
		return fullPath;
	}

	/// <summary>
	/// Creates a directory in the mock filesystem
	/// </summary>
	/// <param name="relativePath">Path relative to the test directory</param>
	/// <returns>Full path to the created directory</returns>
	protected string CreateDirectory(string relativePath)
	{
		var fullPath = Path.Combine(TestDirectory, relativePath);
		MockFileSystem.Directory.CreateDirectory(fullPath);
		return fullPath;
	}
}
