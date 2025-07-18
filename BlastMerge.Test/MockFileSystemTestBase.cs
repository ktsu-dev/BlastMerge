// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
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

		// Note: With the new DI approach, individual test classes should 
		// inject their own IFileSystemProvider instance rather than using static methods
		try
		{
			InitializeFileSystem();
		}
		catch
		{
			// If initialization fails, we don't need to reset anything with DI approach
			throw;
		}
	}

	/// <summary>
	/// Cleans up the mock file system
	/// </summary>
	[TestCleanup]
	public virtual void Cleanup()
	{
		// With dependency injection, no global state to reset
		// MockFileSystem will be garbage collected automatically
	}

	/// <summary>
	/// Override this method to initialize the file system with test data
	/// </summary>
	protected virtual void InitializeFileSystem()
	{
		// To be overridden by derived classes
	}

	/// <summary>
	/// Adds a file to the mock file system
	/// </summary>
	/// <param name="path">The file path</param>
	/// <param name="content">The file content</param>
	protected void AddFile(string path, string content)
	{
		MockFileSystem.AddFile(path, new MockFileData(content));
	}

	/// <summary>
	/// Adds a directory to the mock file system
	/// </summary>
	/// <param name="path">The directory path</param>
	protected void AddDirectory(string path)
	{
		MockFileSystem.AddDirectory(path);
	}

	/// <summary>
	/// Gets a file system path under the test directory
	/// </summary>
	/// <param name="relativePath">The relative path within the test directory</param>
	/// <returns>The full path under the test directory</returns>
	protected string GetTestPath(string relativePath)
	{
		return Path.Combine(TestDirectory, relativePath);
	}
}
