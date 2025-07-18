// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for AppDataBatchManager using static methods
/// </summary>
[TestClass]
public class AppDataBatchManagerTests : DependencyInjectionTestBase
{
	protected override void InitializeTestData()
	{
		// No initialization needed for static class
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
	public void LoadBatch_ExistingBatch_ReturnsCorrectBatch()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "LoadTestBatch",
			FilePatterns = ["*.xml", "*.json"],
			SkipEmptyPatterns = false
		};

		AppDataBatchManager.SaveBatch(originalBatch);

		// Act
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch("LoadTestBatch");

		// Assert
		Assert.IsNotNull(loadedBatch);
		Assert.AreEqual("LoadTestBatch", loadedBatch.Name);
		Assert.AreEqual(2, loadedBatch.FilePatterns.Count);
		Assert.IsTrue(loadedBatch.FilePatterns.Contains("*.xml"));
		Assert.IsTrue(loadedBatch.FilePatterns.Contains("*.json"));
		Assert.IsFalse(loadedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public void LoadBatch_NonExistentBatch_ReturnsNull()
	{
		// Act
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch("NonExistentBatch");

		// Assert
		Assert.IsNull(loadedBatch);
	}

	[TestMethod]
	public void DeleteBatch_ExistingBatch_DeletesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "DeleteTestBatch",
			FilePatterns = ["*.log"],
			SkipEmptyPatterns = true
		};

		AppDataBatchManager.SaveBatch(batch);

		// Act
		bool result = AppDataBatchManager.DeleteBatch("DeleteTestBatch");

		// Assert
		Assert.IsTrue(result);
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch("DeleteTestBatch");
		Assert.IsNull(loadedBatch);
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
	public void ListBatches_WithMultipleBatches_ReturnsAllBatchNames()
	{
		// Arrange
		BatchConfiguration batch1 = new()
		{
			Name = "ListTestBatch1",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = true
		};

		BatchConfiguration batch2 = new()
		{
			Name = "ListTestBatch2",
			FilePatterns = ["*.cs"],
			SkipEmptyPatterns = false
		};

		AppDataBatchManager.SaveBatch(batch1);
		AppDataBatchManager.SaveBatch(batch2);

		// Act
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();

		// Assert
		Assert.IsTrue(batchNames.Contains("ListTestBatch1"));
		Assert.IsTrue(batchNames.Contains("ListTestBatch2"));
	}

	[TestMethod]
	public void GetMostRecentBatch_WithRecentBatch_ReturnsCorrectBatch()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "RecentTestBatch",
			FilePatterns = ["*.md"],
			SkipEmptyPatterns = true
		};

		AppDataBatchManager.SaveBatch(batch);
		AppDataBatchManager.RecordBatchUsage("RecentTestBatch");

		// Act
		string? mostRecentBatch = AppDataBatchManager.MostRecentBatch;

		// Assert
		Assert.AreEqual("RecentTestBatch", mostRecentBatch);
	}

	[TestMethod]
	public void GetMostRecentBatch_WithNoRecentBatch_ReturnsNull()
	{
		// Act
		string? mostRecentBatch = AppDataBatchManager.MostRecentBatch;

		// Assert
		Assert.IsNull(mostRecentBatch);
	}

	[TestMethod]
	public void RecordBatchUsage_WithValidBatch_UpdatesRecentBatch()
	{
		// Arrange
		BatchConfiguration batch1 = new()
		{
			Name = "UsageTestBatch1",
			FilePatterns = ["*.html"],
			SkipEmptyPatterns = true
		};

		BatchConfiguration batch2 = new()
		{
			Name = "UsageTestBatch2",
			FilePatterns = ["*.css"],
			SkipEmptyPatterns = false
		};

		AppDataBatchManager.SaveBatch(batch1);
		AppDataBatchManager.SaveBatch(batch2);

		// Act
		AppDataBatchManager.RecordBatchUsage("UsageTestBatch1");
		string? mostRecent1 = AppDataBatchManager.MostRecentBatch;

		AppDataBatchManager.RecordBatchUsage("UsageTestBatch2");
		string? mostRecent2 = AppDataBatchManager.MostRecentBatch;

		// Assert
		Assert.AreEqual("UsageTestBatch1", mostRecent1);
		Assert.AreEqual("UsageTestBatch2", mostRecent2);
	}

	[TestMethod]
	public void SaveBatch_OverwriteExistingBatch_UpdatesSuccessfully()
	{
		// Arrange
		BatchConfiguration originalBatch = new()
		{
			Name = "OverwriteTestBatch",
			FilePatterns = ["*.original"],
			SkipEmptyPatterns = true
		};

		BatchConfiguration updatedBatch = new()
		{
			Name = "OverwriteTestBatch",
			FilePatterns = ["*.updated", "*.new"],
			SkipEmptyPatterns = false
		};

		AppDataBatchManager.SaveBatch(originalBatch);

		// Act
		AppDataBatchManager.SaveBatch(updatedBatch);

		// Assert
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch("OverwriteTestBatch");
		Assert.IsNotNull(loadedBatch);
		Assert.AreEqual(2, loadedBatch.FilePatterns.Count);
		Assert.IsTrue(loadedBatch.FilePatterns.Contains("*.updated"));
		Assert.IsTrue(loadedBatch.FilePatterns.Contains("*.new"));
		Assert.IsFalse(loadedBatch.SkipEmptyPatterns);
	}

	[TestMethod]
	public void SaveBatch_InvalidBatch_DoesNotSave()
	{
		// Arrange
		BatchConfiguration invalidBatch = new()
		{
			Name = "", // Invalid - empty name
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = true
		};

		// Act
		AppDataBatchManager.SaveBatch(invalidBatch);

		// Assert
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();
		Assert.IsFalse(batchNames.Contains(""));
	}

	[TestMethod]
	public void BatchOperations_WithComplexBatch_WorksCorrectly()
	{
		// Arrange
		BatchConfiguration complexBatch = new()
		{
			Name = "ComplexTestBatch",
			FilePatterns = ["*.txt", "*.cs", "*.xml", "*.json"],
			SkipEmptyPatterns = true,
			SearchPaths = [@"C:\Projects", @"D:\Source", @"E:\Backup"],
			PathExclusionPatterns = ["bin", "obj", "Debug", "Release"]
		};

		// Act
		AppDataBatchManager.SaveBatch(complexBatch);
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch("ComplexTestBatch");

		// Assert
		Assert.IsNotNull(loadedBatch);
		Assert.AreEqual("ComplexTestBatch", loadedBatch.Name);
		Assert.AreEqual(4, loadedBatch.FilePatterns.Count);
		Assert.AreEqual(3, loadedBatch.SearchPaths.Count);
		Assert.AreEqual(4, loadedBatch.PathExclusionPatterns.Count);
		Assert.IsTrue(loadedBatch.SkipEmptyPatterns);
		Assert.IsTrue(loadedBatch.SearchPaths.Contains(@"C:\Projects"));
		Assert.IsTrue(loadedBatch.PathExclusionPatterns.Contains("bin"));
	}

	[TestMethod]
	public void BatchOperations_MultipleOperations_MaintainDataIntegrity()
	{
		// Arrange
		BatchConfiguration[] batches = [
			new() { Name = "Batch1", FilePatterns = ["*.txt"], SkipEmptyPatterns = true },
			new() { Name = "Batch2", FilePatterns = ["*.cs"], SkipEmptyPatterns = false },
			new() { Name = "Batch3", FilePatterns = ["*.xml"], SkipEmptyPatterns = true }
		];

		// Act
		foreach (BatchConfiguration batch in batches)
		{
			AppDataBatchManager.SaveBatch(batch);
		}

		// Delete middle batch
		AppDataBatchManager.DeleteBatch("Batch2");

		// Assert
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();
		Assert.IsTrue(batchNames.Contains("Batch1"));
		Assert.IsFalse(batchNames.Contains("Batch2"));
		Assert.IsTrue(batchNames.Contains("Batch3"));

		// Verify remaining batches are intact
		BatchConfiguration? batch1 = AppDataBatchManager.LoadBatch("Batch1");
		BatchConfiguration? batch3 = AppDataBatchManager.LoadBatch("Batch3");

		Assert.IsNotNull(batch1);
		Assert.IsNotNull(batch3);
		Assert.IsTrue(batch1.SkipEmptyPatterns);
		Assert.IsTrue(batch3.SkipEmptyPatterns);
	}
}
