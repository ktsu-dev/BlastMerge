// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Simple test to verify dependency injection is working properly
/// </summary>
[TestClass]
public class SimpleDependencyInjectionTest : DependencyInjectionTestBase
{
	[TestMethod]
	public void SimpleServiceResolution_ShouldResolveAllCoreServices()
	{
		// Test that all core services can be resolved
		FileHasher fileHasher = GetService<FileHasher>();
		FileFinder fileFinder = GetService<FileFinder>();
		FileDiffer fileDiffer = GetService<FileDiffer>();
		BlastMergeAppData appData = GetService<BlastMergeAppData>();

		Assert.IsNotNull(fileHasher);
		Assert.IsNotNull(fileFinder);
		Assert.IsNotNull(fileDiffer);
		Assert.IsNotNull(appData);
	}

	[TestMethod]
	public void AppDataBatchManager_ShouldWorkWithStaticMethods()
	{
		// Test that AppDataBatchManager static methods work
		BatchConfiguration batchConfig = new()
		{
			Name = "Test Batch",
			Description = "Test batch configuration",
			FilePatterns = ["*.txt"],
			SearchPaths = [@"C:\test"],
			PathExclusionPatterns = []
		};

		// Use static methods since AppDataBatchManager is static
		AppDataBatchManager.SaveBatch(batchConfig);
		BatchConfiguration? loadedBatch = AppDataBatchManager.LoadBatch(batchConfig.Name);

		Assert.IsNotNull(loadedBatch);
		Assert.AreEqual(batchConfig.Name, loadedBatch.Name);
		Assert.AreEqual(batchConfig.Description, loadedBatch.Description);
	}

	[TestMethod]
	public void BlastMergeAppData_ShouldBeInitialized()
	{
		// Test that BlastMergeAppData is properly initialized
		BlastMergeAppData appData = GetService<BlastMergeAppData>();

		Assert.IsNotNull(appData);
		Assert.IsNotNull(appData.BatchConfigurations);
		Assert.IsNotNull(appData.InputHistory);
		Assert.IsNotNull(appData.Settings);
	}
}
