// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the AppDataBatchManager utility class
/// </summary>
[TestClass]
public class AppDataBatchManagerTests
{
	private string _originalAppDataPath = string.Empty;
	private string _testAppDataPath = string.Empty;

	[TestInitialize]
	public void Setup()
	{
		// Reset the singleton instance to ensure test isolation
		BlastMergeAppData.ResetForTesting();

		// Create a temporary app data directory for testing
		_testAppDataPath = SecureTempFileHelper.CreateTempDirectory();

		// Store original environment variable if it exists
		_originalAppDataPath = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty;

		// Set test app data path
		Environment.SetEnvironmentVariable("LOCALAPPDATA", _testAppDataPath);
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Reset the singleton instance to ensure test isolation
		BlastMergeAppData.ResetForTesting();

		// Restore original environment variable
		if (!string.IsNullOrEmpty(_originalAppDataPath))
		{
			Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalAppDataPath);
		}
		else
		{
			Environment.SetEnvironmentVariable("LOCALAPPDATA", null);
		}

		// Clean up test directory
		SecureTempFileHelper.SafeDeleteTempDirectory(_testAppDataPath);
	}

	[TestMethod]
	public void SaveBatch_ValidBatch_SavesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};

		// Act
		AppDataBatchManager.SaveBatch(batch);

		// Assert
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();
		Assert.IsTrue(batchNames.Contains("TestBatch"));
	}

	[TestMethod]
	public void GetBatch_ExistingBatch_ReturnsCorrectBatch()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};
		AppDataBatchManager.SaveBatch(originalBatch);

		// Act
		BatchConfiguration? retrievedBatch = AppDataBatchManager.LoadBatch("TestBatch");

		// Assert
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual("TestBatch", retrievedBatch.Name);
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.txt"));
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.cs"));
		Assert.AreEqual(true, retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public void GetBatch_NonExistentBatch_ReturnsNull()
	{
		// Act
		BatchConfiguration? batch = AppDataBatchManager.LoadBatch("NonExistentBatch");

		// Assert
		Assert.IsNull(batch);
	}

	[TestMethod]
	public void ListBatches_NoBatches_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();

		// Assert
		Assert.IsNotNull(batchNames);
		Assert.AreEqual(0, batchNames.Count);
	}

	[TestMethod]
	public void ListBatches_MultipleBatches_ReturnsAllBatchNames()
	{
		// Arrange
		BatchConfiguration batch1 = new()
		{
			Name = "Batch1",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};

		BatchConfiguration batch2 = new()
		{
			Name = "Batch2",
			FilePatterns = ["*.cs"],
			SkipEmptyPatterns = true
		};

		AppDataBatchManager.SaveBatch(batch1);
		AppDataBatchManager.SaveBatch(batch2);

		// Act
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();

		// Assert
		Assert.AreEqual(2, batchNames.Count);
		Assert.IsTrue(batchNames.Contains("Batch1"));
		Assert.IsTrue(batchNames.Contains("Batch2"));
	}

	[TestMethod]
	public void GetAllBatches_MultipleBatches_ReturnsAllBatches()
	{
		// Arrange
		BatchConfiguration batch1 = new()
		{
			Name = "Batch1",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};

		BatchConfiguration batch2 = new()
		{
			Name = "Batch2",
			FilePatterns = ["*.cs"],
			SkipEmptyPatterns = true
		};

		AppDataBatchManager.SaveBatch(batch1);
		AppDataBatchManager.SaveBatch(batch2);

		// Act
		IReadOnlyCollection<BatchConfiguration> batches = AppDataBatchManager.GetAllBatches();

		// Assert
		Assert.AreEqual(2, batches.Count);

		BatchConfiguration? retrievedBatch1 = batches.FirstOrDefault(b => b.Name == "Batch1");
		BatchConfiguration? retrievedBatch2 = batches.FirstOrDefault(b => b.Name == "Batch2");

		Assert.IsNotNull(retrievedBatch1);
		Assert.IsNotNull(retrievedBatch2);
		Assert.AreEqual(false, retrievedBatch1.SkipEmptyPatterns);
		Assert.AreEqual(true, retrievedBatch2.SkipEmptyPatterns);
	}

	[TestMethod]
	public void DeleteBatch_ExistingBatch_DeletesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};
		AppDataBatchManager.SaveBatch(batch);

		// Verify it exists first
		Assert.IsTrue(AppDataBatchManager.ListBatches().Contains("TestBatch"));

		// Act
		bool result = AppDataBatchManager.DeleteBatch("TestBatch");

		// Assert
		Assert.IsTrue(result);
		Assert.IsFalse(AppDataBatchManager.ListBatches().Contains("TestBatch"));
	}

	[TestMethod]
	public void DeleteBatch_NonExistentBatch_ReturnsFalse()
	{
		// Act
		bool result = AppDataBatchManager.DeleteBatch("NonExistentBatch");

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void SaveBatch_NullBatch_ThrowsArgumentNullException()
	{
		// Act
		AppDataBatchManager.SaveBatch(null!);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void SaveBatch_EmptyBatchName_ThrowsArgumentException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = string.Empty,
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};

		// Act
		AppDataBatchManager.SaveBatch(batch);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void GetBatch_EmptyBatchName_ThrowsArgumentException()
	{
		// Act
		AppDataBatchManager.LoadBatch(string.Empty);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void LoadBatch_NullBatchName_ThrowsArgumentNullException()
	{
		// Act
		AppDataBatchManager.LoadBatch(null!);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentException))]
	public void DeleteBatch_EmptyBatchName_ThrowsArgumentException()
	{
		// Act
		AppDataBatchManager.DeleteBatch(string.Empty);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void DeleteBatch_NullBatchName_ThrowsArgumentNullException()
	{
		// Act
		AppDataBatchManager.DeleteBatch(null!);
	}

	[TestMethod]
	public void SaveBatch_UpdateExistingBatch_UpdatesSuccessfully()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};
		AppDataBatchManager.SaveBatch(originalBatch);

		BatchConfiguration updatedBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};

		// Act
		AppDataBatchManager.SaveBatch(updatedBatch);

		// Assert
		BatchConfiguration? retrievedBatch = AppDataBatchManager.LoadBatch("TestBatch");
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual(2, retrievedBatch.FilePatterns.Count);
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.cs"));
		Assert.AreEqual(true, retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public void SaveBatch_ComplexBatch_SavesAndRetrievesCorrectly()
	{
		// Arrange
		BatchConfiguration complexBatch = new()
		{
			Name = "ComplexBatch",
			FilePatterns = ["*.txt", "*.cs", "*.json", "**/*.config"],
			SkipEmptyPatterns = true
		};

		// Act
		AppDataBatchManager.SaveBatch(complexBatch);
		BatchConfiguration? retrievedBatch = AppDataBatchManager.LoadBatch("ComplexBatch");

		// Assert
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual("ComplexBatch", retrievedBatch.Name);
		Assert.AreEqual(4, retrievedBatch.FilePatterns.Count);
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("**/*.config"));
		Assert.AreEqual(true, retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public void GetAllBatches_NoBatches_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<BatchConfiguration> batches = AppDataBatchManager.GetAllBatches();

		// Assert
		Assert.IsNotNull(batches);
		Assert.AreEqual(0, batches.Count);
	}
}
