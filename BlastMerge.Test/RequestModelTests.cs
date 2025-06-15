// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.ObjectModel;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for request models and batch configuration
/// </summary>
[TestClass]
public class RequestModelTests
{
	#region ProcessingRequest Tests

	/// <summary>
	/// Tests that ProcessingRequest with valid parameters is valid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithValidParameters_IsValid()
	{
		// Arrange
		ProcessingRequest request = new("C:\\test\\directory", "*.txt");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsTrue(isValid);
		Assert.AreEqual("C:\\test\\directory", request.Directory);
		Assert.AreEqual("*.txt", request.FileName);
	}

	/// <summary>
	/// Tests that ProcessingRequest with null directory is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithNullDirectory_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new(null!, "*.txt");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that ProcessingRequest with empty directory is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithEmptyDirectory_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new("", "*.txt");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that ProcessingRequest with whitespace directory is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithWhitespaceDirectory_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new("   ", "*.txt");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that ProcessingRequest with null filename is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithNullFileName_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new("C:\\test\\directory", null!);

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that ProcessingRequest with empty filename is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithEmptyFileName_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new("C:\\test\\directory", "");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that ProcessingRequest with whitespace filename is invalid.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_WithWhitespaceFileName_IsInvalid()
	{
		// Arrange
		ProcessingRequest request = new("C:\\test\\directory", "   ");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests ProcessingRequest record equality.
	/// </summary>
	[TestMethod]
	public void ProcessingRequest_RecordEquality_WorksCorrectly()
	{
		// Arrange
		ProcessingRequest request1 = new("C:\\test", "*.txt");
		ProcessingRequest request2 = new("C:\\test", "*.txt");
		ProcessingRequest request3 = new("C:\\other", "*.txt");

		// Act & Assert
		Assert.AreEqual(request1, request2);
		Assert.AreNotEqual(request1, request3);
		Assert.AreEqual(request1.GetHashCode(), request2.GetHashCode());
	}

	#endregion

	#region BatchRequest Tests

	/// <summary>
	/// Tests that BatchRequest with valid parameters is valid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithValidParameters_IsValid()
	{
		// Arrange
		BatchRequest request = new("C:\\test\\directory", "TestBatch");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsTrue(isValid);
		Assert.AreEqual("C:\\test\\directory", request.Directory);
		Assert.AreEqual("TestBatch", request.BatchName);
	}

	/// <summary>
	/// Tests that BatchRequest with null directory is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithNullDirectory_IsInvalid()
	{
		// Arrange
		BatchRequest request = new(null!, "TestBatch");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that BatchRequest with empty directory is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithEmptyDirectory_IsInvalid()
	{
		// Arrange
		BatchRequest request = new("", "TestBatch");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that BatchRequest with whitespace directory is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithWhitespaceDirectory_IsInvalid()
	{
		// Arrange
		BatchRequest request = new("   ", "TestBatch");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that BatchRequest with null batch name is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithNullBatchName_IsInvalid()
	{
		// Arrange
		BatchRequest request = new("C:\\test\\directory", null!);

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that BatchRequest with empty batch name is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithEmptyBatchName_IsInvalid()
	{
		// Arrange
		BatchRequest request = new("C:\\test\\directory", "");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests that BatchRequest with whitespace batch name is invalid.
	/// </summary>
	[TestMethod]
	public void BatchRequest_WithWhitespaceBatchName_IsInvalid()
	{
		// Arrange
		BatchRequest request = new("C:\\test\\directory", "   ");

		// Act
		bool isValid = request.IsValid();

		// Assert
		Assert.IsFalse(isValid);
	}

	/// <summary>
	/// Tests BatchRequest record equality.
	/// </summary>
	[TestMethod]
	public void BatchRequest_RecordEquality_WorksCorrectly()
	{
		// Arrange
		BatchRequest request1 = new("C:\\test", "BatchA");
		BatchRequest request2 = new("C:\\test", "BatchA");
		BatchRequest request3 = new("C:\\test", "BatchB");

		// Act & Assert
		Assert.AreEqual(request1, request2);
		Assert.AreNotEqual(request1, request3);
		Assert.AreEqual(request1.GetHashCode(), request2.GetHashCode());
	}

	#endregion

	#region BatchConfiguration Tests

	/// <summary>
	/// Tests BatchConfiguration default values.
	/// </summary>
	[TestMethod]
	public void BatchConfiguration_DefaultValues_AreCorrect()
	{
		// Act
		BatchConfiguration config = new();

		// Assert
		Assert.AreEqual(string.Empty, config.Name);
		Assert.AreEqual(string.Empty, config.Description);
		Assert.IsNotNull(config.FilePatterns);
		Assert.AreEqual(0, config.FilePatterns.Count);
		Assert.IsNotNull(config.SearchPaths);
		Assert.AreEqual(0, config.SearchPaths.Count);
		Assert.IsNotNull(config.PathExclusionPatterns);
		Assert.AreEqual(0, config.PathExclusionPatterns.Count);
		Assert.IsTrue(config.CreatedDate <= DateTime.UtcNow);
		Assert.IsTrue(config.LastModified <= DateTime.UtcNow);
	}

	/// <summary>
	/// Tests BatchConfiguration with all properties set.
	/// </summary>
	[TestMethod]
	public void BatchConfiguration_WithAllProperties_StoresCorrectly()
	{
		// Arrange
		DateTime testDate = new(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		Collection<string> patterns = ["*.txt", "*.cs"];
		Collection<string> searchPaths = ["C:\\src", "C:\\tests"];
		Collection<string> exclusions = ["*\\bin\\*", "*\\obj\\*"];

		// Act
		BatchConfiguration config = new()
		{
			Name = "TestBatch",
			Description = "Test batch configuration",
			FilePatterns = patterns,
			SearchPaths = searchPaths,
			PathExclusionPatterns = exclusions,
			CreatedDate = testDate,
			LastModified = testDate
		};

		// Assert
		Assert.AreEqual("TestBatch", config.Name);
		Assert.AreEqual("Test batch configuration", config.Description);
		Assert.AreSame(patterns, config.FilePatterns);
		Assert.AreEqual(2, config.FilePatterns.Count);
		Assert.IsTrue(config.FilePatterns.Contains("*.txt"));
		Assert.IsTrue(config.FilePatterns.Contains("*.cs"));
		Assert.AreSame(searchPaths, config.SearchPaths);
		Assert.AreEqual(2, config.SearchPaths.Count);
		Assert.IsTrue(config.SearchPaths.Contains("C:\\src"));
		Assert.IsTrue(config.SearchPaths.Contains("C:\\tests"));
		Assert.AreSame(exclusions, config.PathExclusionPatterns);
		Assert.AreEqual(2, config.PathExclusionPatterns.Count);
		Assert.IsTrue(config.PathExclusionPatterns.Contains("*\\bin\\*"));
		Assert.IsTrue(config.PathExclusionPatterns.Contains("*\\obj\\*"));
		Assert.AreEqual(testDate, config.CreatedDate);
		Assert.AreEqual(testDate, config.LastModified);
	}

	/// <summary>
	/// Tests BatchConfiguration collections are mutable.
	/// </summary>
	[TestMethod]
	public void BatchConfiguration_Collections_AreMutable()
	{
		// Arrange
		BatchConfiguration config = new();

		// Act
		config.FilePatterns.Add("*.txt");
		config.SearchPaths.Add("C:\\test");
		config.PathExclusionPatterns.Add("*\\temp\\*");

		// Assert
		Assert.AreEqual(1, config.FilePatterns.Count);
		Assert.AreEqual("*.txt", config.FilePatterns[0]);
		Assert.AreEqual(1, config.SearchPaths.Count);
		Assert.AreEqual("C:\\test", config.SearchPaths[0]);
		Assert.AreEqual(1, config.PathExclusionPatterns.Count);
		Assert.AreEqual("*\\temp\\*", config.PathExclusionPatterns[0]);
	}

	/// <summary>
	/// Tests BatchConfiguration with empty collections.
	/// </summary>
	[TestMethod]
	public void BatchConfiguration_WithEmptyCollections_HandlesCorrectly()
	{
		// Arrange
		BatchConfiguration config = new()
		{
			Name = "EmptyBatch",
			FilePatterns = [],
			SearchPaths = [],
			PathExclusionPatterns = []
		};

		// Act & Assert
		Assert.AreEqual("EmptyBatch", config.Name);
		Assert.IsNotNull(config.FilePatterns);
		Assert.AreEqual(0, config.FilePatterns.Count);
		Assert.IsNotNull(config.SearchPaths);
		Assert.AreEqual(0, config.SearchPaths.Count);
		Assert.IsNotNull(config.PathExclusionPatterns);
		Assert.AreEqual(0, config.PathExclusionPatterns.Count);
	}

	/// <summary>
	/// Tests BatchConfiguration date properties can be modified.
	/// </summary>
	[TestMethod]
	public void BatchConfiguration_DateProperties_CanBeModified()
	{
		// Arrange
		BatchConfiguration config = new();
		DateTime originalCreated = config.CreatedDate;
		DateTime originalModified = config.LastModified;
		DateTime newDate = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

		// Act
		config.CreatedDate = newDate;
		config.LastModified = newDate.AddHours(1);

		// Assert
		Assert.AreNotEqual(originalCreated, config.CreatedDate);
		Assert.AreNotEqual(originalModified, config.LastModified);
		Assert.AreEqual(newDate, config.CreatedDate);
		Assert.AreEqual(newDate.AddHours(1), config.LastModified);
	}

	#endregion
}