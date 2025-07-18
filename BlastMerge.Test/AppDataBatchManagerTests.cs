// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for AppDataBatchManager using dependency injection
/// </summary>
[TestClass]
public class AppDataBatchManagerTests : DependencyInjectionTestBase
{
	private AppDataBatchManager _batchManager = null!;

	protected override void InitializeTestData()
	{
		_batchManager = GetService<AppDataBatchManager>();
	}

	[TestMethod]
	public async Task SaveBatchAsync_ValidBatch_SavesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};

		// Act
		bool result = await _batchManager.SaveBatchAsync(batch).ConfigureAwait(false);

		// Assert
		Assert.IsTrue(result);
		IReadOnlyCollection<string> batchNames = await _batchManager.ListBatchesAsync().ConfigureAwait(false);
		Assert.IsTrue(batchNames.Contains("TestBatch"));
	}

	[TestMethod]
	public async Task LoadBatchAsync_ExistingBatch_ReturnsCorrectBatch()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};
		await _batchManager.SaveBatchAsync(originalBatch).ConfigureAwait(false);

		// Act
		BatchConfiguration? retrievedBatch = await _batchManager.LoadBatchAsync("TestBatch").ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual("TestBatch", retrievedBatch.Name);
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.txt"));
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.cs"));
		Assert.AreEqual(true, retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public async Task LoadBatchAsync_NonExistentBatch_ReturnsNull()
	{
		// Act
		BatchConfiguration? batch = await _batchManager.LoadBatchAsync("NonExistentBatch").ConfigureAwait(false);

		// Assert
		Assert.IsNull(batch);
	}

	[TestMethod]
	public async Task ListBatchesAsync_NoBatches_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<string> batchNames = await _batchManager.ListBatchesAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(batchNames);
		Assert.AreEqual(0, batchNames.Count);
	}

	[TestMethod]
	public async Task ListBatchesAsync_MultipleBatches_ReturnsAllBatchNames()
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

		await _batchManager.SaveBatchAsync(batch1).ConfigureAwait(false);
		await _batchManager.SaveBatchAsync(batch2).ConfigureAwait(false);

		// Act
		IReadOnlyCollection<string> batchNames = await _batchManager.ListBatchesAsync().ConfigureAwait(false);

		// Assert
		Assert.AreEqual(2, batchNames.Count);
		Assert.IsTrue(batchNames.Contains("Batch1"));
		Assert.IsTrue(batchNames.Contains("Batch2"));
	}

	[TestMethod]
	public async Task GetAllBatchesAsync_MultipleBatches_ReturnsAllBatches()
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

		await _batchManager.SaveBatchAsync(batch1).ConfigureAwait(false);
		await _batchManager.SaveBatchAsync(batch2).ConfigureAwait(false);

		// Act
		IReadOnlyCollection<BatchConfiguration> batches = await _batchManager.GetAllBatchesAsync().ConfigureAwait(false);

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
	public async Task DeleteBatchAsync_ExistingBatch_DeletesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};
		await _batchManager.SaveBatchAsync(batch).ConfigureAwait(false);

		// Verify it exists first
		IReadOnlyCollection<string> batchesBeforeDelete = await _batchManager.ListBatchesAsync().ConfigureAwait(false);
		Assert.IsTrue(batchesBeforeDelete.Contains("TestBatch"));

		// Act
		bool result = await _batchManager.DeleteBatchAsync("TestBatch").ConfigureAwait(false);

		// Assert
		Assert.IsTrue(result);
		IReadOnlyCollection<string> batchesAfterDelete = await _batchManager.ListBatchesAsync().ConfigureAwait(false);
		Assert.IsFalse(batchesAfterDelete.Contains("TestBatch"));
	}

	[TestMethod]
	public async Task DeleteBatchAsync_NonExistentBatch_ReturnsFalse()
	{
		// Act
		bool result = await _batchManager.DeleteBatchAsync("NonExistentBatch").ConfigureAwait(false);

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task SaveBatchAsync_NullBatch_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _batchManager.SaveBatchAsync(null!)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task SaveBatchAsync_EmptyBatchName_ThrowsArgumentException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = string.Empty,
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};

		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => _batchManager.SaveBatchAsync(batch)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task LoadBatchAsync_EmptyBatchName_ThrowsArgumentException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => _batchManager.LoadBatchAsync(string.Empty)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task LoadBatchAsync_NullBatchName_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _batchManager.LoadBatchAsync(null!)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task DeleteBatchAsync_EmptyBatchName_ThrowsArgumentException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => _batchManager.DeleteBatchAsync(string.Empty)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task DeleteBatchAsync_NullBatchName_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _batchManager.DeleteBatchAsync(null!)).ConfigureAwait(false);
	}

	[TestMethod]
	public async Task SaveBatchAsync_UpdateExistingBatch_UpdatesSuccessfully()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};
		await _batchManager.SaveBatchAsync(originalBatch).ConfigureAwait(false);

		BatchConfiguration updatedBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt", "*.cs"],
			SkipEmptyPatterns = true
		};

		// Act
		await _batchManager.SaveBatchAsync(updatedBatch).ConfigureAwait(false);

		// Assert
		BatchConfiguration? retrievedBatch = await _batchManager.LoadBatchAsync("TestBatch").ConfigureAwait(false);
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual(2, retrievedBatch.FilePatterns.Count);
		Assert.IsTrue(retrievedBatch.FilePatterns.Contains("*.cs"));
		Assert.AreEqual(true, retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public async Task SaveBatchAsync_ComplexBatch_SavesAndRetrievesCorrectly()
	{
		// Arrange
		BatchConfiguration complexBatch = new()
		{
			Name = "ComplexBatch",
			FilePatterns = ["*.txt", "*.cs", "*.json"],
			SkipEmptyPatterns = true,
			SearchPaths = [@"C:\Source", @"D:\Projects"],
			PathExclusionPatterns = ["*/bin/*", "*/obj/*", "*node_modules*"]
		};

		// Act
		await _batchManager.SaveBatchAsync(complexBatch).ConfigureAwait(false);
		BatchConfiguration? retrievedBatch = await _batchManager.LoadBatchAsync("ComplexBatch").ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(retrievedBatch);
		Assert.AreEqual("ComplexBatch", retrievedBatch.Name);
		Assert.AreEqual(3, retrievedBatch.FilePatterns.Count);
		Assert.AreEqual(2, retrievedBatch.SearchPaths.Count);
		Assert.AreEqual(3, retrievedBatch.PathExclusionPatterns.Count);
		Assert.IsTrue(retrievedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public async Task GetAllBatchesAsync_NoBatches_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<BatchConfiguration> batches = await _batchManager.GetAllBatchesAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(batches);
		Assert.AreEqual(0, batches.Count);
	}

	[TestMethod]
	public async Task RecordBatchUsageAsync_ValidBatch_RecordsUsage()
	{
		// Arrange
		const string batchName = "TestBatch";

		// Act
		await _batchManager.RecordBatchUsageAsync(batchName).ConfigureAwait(false);

		// Assert
		string? recentBatch = await _batchManager.GetMostRecentBatchAsync().ConfigureAwait(false);
		Assert.AreEqual(batchName, recentBatch);
	}

	[TestMethod]
	public async Task GetMostRecentBatchAsync_NoUsage_ReturnsNull()
	{
		// Act
		string? recentBatch = await _batchManager.GetMostRecentBatchAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNull(recentBatch);
	}

	[TestMethod]
	public async Task CreateDefaultBatchIfNoneExistAsync_NoBatches_CreatesDefault()
	{
		// Act
		bool result = await _batchManager.CreateDefaultBatchIfNoneExistAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(result);
		IReadOnlyCollection<string> batches = await _batchManager.ListBatchesAsync().ConfigureAwait(false);
		Assert.AreEqual(1, batches.Count);
	}

	[TestMethod]
	public async Task CreateDefaultBatchIfNoneExistAsync_ExistingBatches_DoesNotCreateDefault()
	{
		// Arrange
		BatchConfiguration existingBatch = new()
		{
			Name = "ExistingBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};
		await _batchManager.SaveBatchAsync(existingBatch).ConfigureAwait(false);

		// Act
		bool result = await _batchManager.CreateDefaultBatchIfNoneExistAsync().ConfigureAwait(false);

		// Assert
		Assert.IsFalse(result);
		IReadOnlyCollection<string> batches = await _batchManager.ListBatchesAsync().ConfigureAwait(false);
		Assert.AreEqual(1, batches.Count);
		Assert.IsTrue(batches.Contains("ExistingBatch"));
	}
}
