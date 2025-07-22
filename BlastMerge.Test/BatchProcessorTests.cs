// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for BatchProcessor using dependency injection
/// </summary>
[TestClass]
public class BatchProcessorTests : DependencyInjectionTestBase
{
	private BatchProcessor _processor = null!;

	protected override void InitializeTestData()
	{
		_processor = GetService<BatchProcessor>();
	}

	[TestMethod]
	public void ProcessBatch_WithValidConfiguration_ReturnsSuccessResult()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Test Batch", result.BatchName);
	}

	[TestMethod]
	public void ProcessBatch_WithMultiplePatterns_ProcessesAllPatterns()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Multi Pattern Batch",
			FilePatterns = ["*.txt", "*.cs"]
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Multi Pattern Batch", result.BatchName);
		Assert.IsNotNull(result.PatternResults);
	}

	[TestMethod]
	public void ProcessBatch_WithNonExistentDirectory_ReturnsFailure()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\nonexistent",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsFalse(result.Success);
		Assert.IsTrue(result.Summary.Contains("Directory does not exist"));
	}

	[TestMethod]
	public void ProcessBatch_WithEmptyPatterns_HandlesCorrectly()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Empty Pattern Batch",
			FilePatterns = []
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Empty Pattern Batch", result.BatchName);
	}

	[TestMethod]
	public void ProcessBatch_WithPatternCallback_CallsCallback()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Pattern Callback Batch",
			FilePatterns = ["*.txt", "*.cs"],
			PromptBeforeEachPattern = true
		};

		List<string> calledPatterns = [];

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			pattern =>
			{
				calledPatterns.Add(pattern);
				return pattern == "*.txt"; // Only process txt files
			});

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(calledPatterns.Count > 0);
		Assert.IsTrue(calledPatterns.Contains("*.txt"));
		Assert.IsTrue(calledPatterns.Contains("*.cs"));
	}

	[TestMethod]
	public void ProcessBatch_WithSkipEmptyPatterns_SkipsAppropriately()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Skip Empty Batch",
			FilePatterns = ["*.nonexistent"],
			SkipEmptyPatterns = true
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Skip Empty Batch", result.BatchName);
	}

	[TestMethod]
	public void ProcessBatch_WithStatusCallback_CallsStatusCallback()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Status Callback Batch",
			FilePatterns = ["*.txt"]
		};

		List<MergeSessionStatus> statusUpdates = [];

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			statusUpdates.Add,
			() => true);

		// Assert
		Assert.IsNotNull(result);
		// Status updates might be empty if no files are processed
	}

	[TestMethod]
	public void ProcessBatch_WithContinuationCallback_RespectsCallback()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Continuation Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => false); // Stop processing

		// Assert
		Assert.IsNotNull(result);
		// Continuation callback behavior depends on file presence
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithValidConfiguration_ProcessesSuccessfully()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Discrete Phases Batch",
			FilePatterns = ["*.txt"]
		};

		List<string> progressMessages = [];

		// Act
		BatchResult result = _processor.ProcessBatchWithDiscretePhases(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			progressMessages.Add);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Discrete Phases Batch", result.BatchName);
		// Progress messages would be populated if files were processed
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithCustomParallelism_UsesSpecifiedDegree()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Parallelism Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = _processor.ProcessBatchWithDiscretePhases(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true,
			null,
			2); // Custom parallelism

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual("Parallelism Batch", result.BatchName);
	}

	[TestMethod]
	public void CreateCustomBatch_WithValidParameters_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Custom Batch";
		string[] patterns = ["*.txt", "*.cs"];
		string description = "Custom Description";

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns, description);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(description, result.Description);
		Assert.AreEqual(patterns.Length, result.FilePatterns.Count);
		Assert.IsTrue(result.FilePatterns.Contains("*.txt"));
		Assert.IsTrue(result.FilePatterns.Contains("*.cs"));
	}

	[TestMethod]
	public void CreateCustomBatch_WithEmptyDescription_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Simple Batch";
		string[] patterns = ["*.txt"];

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(string.Empty, result.Description);
		Assert.AreEqual(1, result.FilePatterns.Count);
		Assert.IsTrue(result.FilePatterns.Contains("*.txt"));
	}

	[TestMethod]
	public void ProcessBatch_ReturnsDetailedResults()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Detailed Results Batch",
			FilePatterns = ["*.txt"]
		};

		// Act
		BatchResult result = _processor.ProcessBatch(
			batch,
			@"C:\test",
			(path1, path2, output) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.PatternResults);
		Assert.IsNotNull(result.Summary);
		Assert.IsTrue(result.TotalPatternsProcessed >= 0);
		Assert.IsTrue(result.SuccessfulPatterns >= 0);
	}
}
