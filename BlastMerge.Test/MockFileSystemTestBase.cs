// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Services;
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
	/// A unique identifier for this test instance, used to prevent collisions in parallel tests
	/// </summary>
	protected string TestId { get; } = Guid.NewGuid().ToString("N");

	/// <summary>
	/// Initializes the mock file system
	/// </summary>
	[TestInitialize]
	public virtual void SetUp()
	{
		// Create unique test directory per test to avoid collisions when running in parallel
		TestDirectory = $@"C:\mock-test-dir-{TestId}";

		// Create a fresh mock filesystem instance for this test
		MockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
		MockFileSystem.Directory.CreateDirectory(TestDirectory);

		// Set this test's mock file system as the current one for all services to use
		// Each test gets its own isolated filesystem
		FileSystemProvider.SetFileSystem(MockFileSystem);

		try
		{
			InitializeFileSystem();
		}
		catch
		{
			// If initialization fails, ensure we still reset the filesystem before propagating exception
			FileSystemProvider.ResetToDefault();
			throw;
		}
	}

	/// <summary>
	/// Clean up after tests
	/// </summary>
	[TestCleanup]
	public virtual void Cleanup()
	{
		// Reset the file system back to default after test completes
		// This ensures the next test (or real code) gets a clean slate
		FileSystemProvider.ResetToDefault();
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
		string fullPath = Path.Combine(TestDirectory, relativePath);
		string? directory = Path.GetDirectoryName(fullPath);

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
		string fullPath = Path.Combine(TestDirectory, relativePath);
		MockFileSystem.Directory.CreateDirectory(fullPath);
		return fullPath;
	}
}
