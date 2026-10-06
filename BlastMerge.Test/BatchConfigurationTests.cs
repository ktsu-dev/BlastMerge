// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BatchConfiguration"/>'s factories and validation.
/// </summary>
[TestClass]
public class BatchConfigurationTests
{
	/// <summary>
	/// The default batch covers the common repository files and is valid.
	/// </summary>
	[TestMethod]
	public void CreateDefault_HasCommonRepositoryFiles()
	{
		BatchConfiguration batch = BatchConfiguration.CreateDefault();

		Assert.AreEqual("Common Repository Files", batch.Name);
		Assert.IsFalse(string.IsNullOrWhiteSpace(batch.Description));
		Assert.AreSequenceEqual(
			[".gitignore", ".gitattributes", ".editorconfig", "*.yml", "*.yaml", ".mailmap", "LICENSE", "LICENSE.md", "LICENSE.txt"],
			batch.FilePatterns);
		Assert.IsTrue(batch.SkipEmptyPatterns);
		Assert.IsFalse(batch.PromptBeforeEachPattern);
		Assert.IsTrue(batch.IsValid());
	}

	/// <summary>
	/// The repository sync batch covers configuration, scripts and the licence template, and is valid.
	/// </summary>
	[TestMethod]
	public void CreateRepositorySyncBatch_HasSyncPatterns()
	{
		BatchConfiguration batch = BatchConfiguration.CreateRepositorySyncBatch();

		Assert.AreEqual("Repository Sync Batch", batch.Name);
		Assert.AreSequenceEqual(
			[".runsettings", ".mailmap", ".gitignore", ".gitattributes", ".editorconfig", "*.yml", "icon.png", "*.ps1", "*.psm1", "*.psd1", "LICENSE.template"],
			batch.FilePatterns);
		Assert.IsTrue(batch.SkipEmptyPatterns);
		Assert.IsFalse(batch.PromptBeforeEachPattern);
		Assert.IsTrue(batch.IsValid());
	}

	/// <summary>
	/// A new configuration has no name and no patterns, so it is not valid.
	/// </summary>
	[TestMethod]
	public void NewConfiguration_IsNotValid()
	{
		BatchConfiguration batch = new();

		Assert.IsFalse(batch.IsValid());
		Assert.IsEmpty(batch.SearchPaths);
		Assert.IsEmpty(batch.PathExclusionPatterns);
		Assert.IsTrue(batch.SkipEmptyPatterns);
	}

	/// <summary>
	/// A blank name makes an otherwise complete batch invalid.
	/// </summary>
	[TestMethod]
	public void IsValid_FalseForBlankName()
	{
		BatchConfiguration batch = new() { Name = "   ", FilePatterns = ["*.txt"] };

		Assert.IsFalse(batch.IsValid());
	}

	/// <summary>
	/// A named batch without patterns is invalid.
	/// </summary>
	[TestMethod]
	public void IsValid_FalseWithoutPatterns()
	{
		BatchConfiguration batch = new() { Name = "Batch" };

		Assert.IsFalse(batch.IsValid());
	}

	/// <summary>
	/// A single blank pattern makes the batch invalid.
	/// </summary>
	[TestMethod]
	public void IsValid_FalseWithBlankPattern()
	{
		BatchConfiguration batch = new() { Name = "Batch", FilePatterns = ["*.txt", " "] };

		Assert.IsFalse(batch.IsValid());
	}

	/// <summary>
	/// A named batch whose patterns are all non-blank is valid.
	/// </summary>
	[TestMethod]
	public void IsValid_TrueForNamedBatchWithPatterns()
	{
		BatchConfiguration batch = new() { Name = "Batch", FilePatterns = ["*.txt", "README.md"] };

		Assert.IsTrue(batch.IsValid());
	}
}
