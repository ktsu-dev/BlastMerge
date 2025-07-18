// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for the FileSystemProvider service
/// NOTE: These tests are currently ignored as they test the old static API
/// that has been replaced with dependency injection.
/// </summary>
[TestClass]
public class FileSystemProviderTests
{
	[TestCleanup]
	public void Cleanup()
	{
		// Always reset to default after each test
		// FileSystemProvider.ResetToDefault(); // Not needed with DI
	}

	/// <summary>
	/// Tests that the default file system is the real file system
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void Current_ByDefault_ReturnsRealFileSystem()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that SetFileSystem allows setting a custom file system
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void SetFileSystem_WithMockFileSystem_UpdatesCurrent()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that ResetToDefault restores the real file system
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void ResetToDefault_AfterSettingMockFileSystem_RestoresRealFileSystem()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that multiple SetFileSystem calls work correctly
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void SetFileSystem_MultipleCalls_WorksCorrectly()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that FileSystemProvider is thread-safe
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void FileSystemProvider_ConcurrentAccess_IsThreadSafe()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that SetFileSystem throws on null input
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void SetFileSystem_NullInput_ThrowsArgumentNullException()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests setting file system to real FileSystem instance
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void SetFileSystem_WithRealFileSystem_WorksCorrectly()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests that file operations work with mock file system
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void FileOperations_WithMockFileSystem_WorkCorrectly()
	{
		// This test is no longer relevant with DI approach
	}

	/// <summary>
	/// Tests resetting to default multiple times
	/// </summary>
	[TestMethod]
	[Ignore("Static FileSystemProvider API has been replaced with DI")]
	public void ResetToDefault_MultipleCalls_WorksCorrectly()
	{
		// This test is no longer relevant with DI approach
	}
}
