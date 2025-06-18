// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for the FileSystemProvider service
/// </summary>
[TestClass]
public class FileSystemProviderTests
{
	[TestCleanup]
	public void Cleanup()
	{
		// Always reset to default after each test
		FileSystemProvider.ResetToDefault();
	}

	/// <summary>
	/// Tests that the default file system is the real file system
	/// </summary>
	[TestMethod]
	public void Current_ByDefault_ReturnsRealFileSystem()
	{
		// Act
		IFileSystem current = FileSystemProvider.Current;

		// Assert
		Assert.IsNotNull(current);
		Assert.IsInstanceOfType<FileSystem>(current);
	}

	/// <summary>
	/// Tests that SetFileSystem allows setting a custom file system
	/// </summary>
	[TestMethod]
	public void SetFileSystem_WithMockFileSystem_UpdatesCurrent()
	{
		// Arrange
		MockFileSystem mockFileSystem = new();

		// Act
		FileSystemProvider.SetFileSystem(mockFileSystem);

		// Assert
		IFileSystem current = FileSystemProvider.Current;
		Assert.AreSame(mockFileSystem, current);
	}

	/// <summary>
	/// Tests that ResetToDefault restores the real file system
	/// </summary>
	[TestMethod]
	public void ResetToDefault_AfterSettingMockFileSystem_RestoresRealFileSystem()
	{
		// Arrange
		MockFileSystem mockFileSystem = new();
		FileSystemProvider.SetFileSystem(mockFileSystem);

		// Verify mock is set
		Assert.AreSame(mockFileSystem, FileSystemProvider.Current);

		// Act
		FileSystemProvider.ResetToDefault();

		// Assert
		IFileSystem current = FileSystemProvider.Current;
		Assert.IsNotNull(current);
		Assert.IsInstanceOfType<FileSystem>(current);
		Assert.AreNotSame(mockFileSystem, current);
	}

	/// <summary>
	/// Tests that SetFileSystem throws for null input
	/// </summary>
	[TestMethod]
	public void SetFileSystem_WithNull_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			FileSystemProvider.SetFileSystem(null!));
	}

	/// <summary>
	/// Tests that multiple calls to SetFileSystem work correctly
	/// </summary>
	[TestMethod]
	public void SetFileSystem_MultipleCalls_UpdatesCurrentCorrectly()
	{
		// Arrange
		MockFileSystem mockFileSystem1 = new();
		MockFileSystem mockFileSystem2 = new();

		// Act & Assert - First mock
		FileSystemProvider.SetFileSystem(mockFileSystem1);
		Assert.AreSame(mockFileSystem1, FileSystemProvider.Current);

		// Act & Assert - Second mock
		FileSystemProvider.SetFileSystem(mockFileSystem2);
		Assert.AreSame(mockFileSystem2, FileSystemProvider.Current);
		Assert.AreNotSame(mockFileSystem1, FileSystemProvider.Current);
	}

	/// <summary>
	/// Tests that Current property can be called multiple times safely
	/// </summary>
	[TestMethod]
	public void Current_MultipleCalls_ReturnsSameInstance()
	{
		// Act
		IFileSystem first = FileSystemProvider.Current;
		IFileSystem second = FileSystemProvider.Current;

		// Assert
		Assert.AreSame(first, second);
	}

	/// <summary>
	/// Tests thread safety of file system provider operations
	/// </summary>
	[TestMethod]
	public void FileSystemProvider_ConcurrentOperations_ThreadSafe()
	{
		// Arrange
		MockFileSystem mockFileSystem = new();
		const int threadCount = 10;
		const int operationsPerThread = 100;
		List<Task> tasks = [];
		List<Exception> exceptions = [];
		object lockObject = new();

		// Act - Run concurrent operations
		for (int i = 0; i < threadCount; i++)
		{
			tasks.Add(Task.Run(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						// Alternate between setting mock and resetting to default
						if (j % 2 == 0)
						{
							FileSystemProvider.SetFileSystem(mockFileSystem);
						}
						else
						{
							FileSystemProvider.ResetToDefault();
						}

						// Access Current property
						IFileSystem _ = FileSystemProvider.Current;
					}
				}
				catch (ArgumentNullException ex)
				{
					lock (lockObject)
					{
						exceptions.Add(ex);
					}
				}
				catch (InvalidOperationException ex)
				{
					lock (lockObject)
					{
						exceptions.Add(ex);
					}
				}
			}));
		}

		Task.WaitAll([.. tasks]);

		// Assert
		Assert.AreEqual(0, exceptions.Count, $"Expected no exceptions, but got: {string.Join(", ", exceptions.Select(e => e.Message))}");

		// Final state should be accessible
		IFileSystem final = FileSystemProvider.Current;
		Assert.IsNotNull(final);
	}
}
