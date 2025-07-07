// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Simple test to verify dependency injection works
/// </summary>
[TestClass]
public class SimpleDependencyInjectionTest : DependencyInjectionTestBase
{
	[TestMethod]
	public async Task DependencyInjection_CanCreateServices_Success()
	{
		// Act
		BlastMergePersistenceService persistenceService = GetService<BlastMergePersistenceService>();
		AppDataBatchManager batchManager = GetService<AppDataBatchManager>();

		// Assert
		Assert.IsNotNull(persistenceService);
		Assert.IsNotNull(batchManager);

		// Test basic functionality
		BlastMergeAppData data = await persistenceService.GetAsync();
		Assert.IsNotNull(data);
	}

	[TestMethod]
	public async Task BatchManager_BasicOperations_Work()
	{
		// Arrange
		AppDataBatchManager batchManager = GetService<AppDataBatchManager>();
		BatchConfiguration testBatch = new()
		{
			Name = "TestBatch",
			FilePatterns = ["*.txt"],
			SkipEmptyPatterns = false
		};

		// Act
		bool saveResult = await batchManager.SaveBatchAsync(testBatch);
		BatchConfiguration? loadedBatch = await batchManager.LoadBatchAsync("TestBatch");

		// Assert
		Assert.IsTrue(saveResult);
		Assert.IsNotNull(loadedBatch);
		Assert.AreEqual("TestBatch", loadedBatch.Name);
		Assert.AreEqual(1, loadedBatch.FilePatterns.Count);
		Assert.IsTrue(loadedBatch.FilePatterns.Contains("*.txt"));
	}
} 