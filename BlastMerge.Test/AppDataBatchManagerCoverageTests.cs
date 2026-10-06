// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AppDataStorage = ktsu.AppDataStorage.AppData;

/// <summary>
/// Covers the parts of <see cref="AppDataBatchManager"/> the existing suite leaves untouched: invalid
/// batches, the queued-save path taken when auto-save is off, default batch creation and usage tracking.
/// Application data is stored in a mock file system.
/// </summary>
[TestClass]
[DoNotParallelize]
public class AppDataBatchManagerCoverageTests
{
	private string originalCurrentDirectory = string.Empty;
	private string workingDirectory = string.Empty;

	/// <summary>
	/// Runs from an empty directory, because ktsu.AppDataStorage rejects a storage directory name that
	/// already exists as a file relative to the current directory, which the test apphost does on Unix.
	/// </summary>
	[TestInitialize]
	public void SetUp()
	{
		originalCurrentDirectory = Environment.CurrentDirectory;
		workingDirectory = Directory.CreateTempSubdirectory("blastmerge-batch-").FullName;
		Environment.CurrentDirectory = workingDirectory;
	}

	/// <summary>
	/// Restores the original working directory.
	/// </summary>
	[TestCleanup]
	public void Cleanup()
	{
		Environment.CurrentDirectory = originalCurrentDirectory;
		Directory.Delete(workingDirectory, recursive: true);
	}

	/// <summary>
	/// A batch that fails validation is not saved.
	/// </summary>
	[TestMethod]
	public void SaveBatch_InvalidBatch_ReturnsFalseAndStoresNothing()
	{
		WithMockAppData(() =>
		{
			BatchConfiguration batch = new() { Name = "empty" };

			Assert.IsFalse(AppDataBatchManager.SaveBatch(batch));
			Assert.IsNull(AppDataBatchManager.LoadBatch("empty"));
		});
	}

	/// <summary>
	/// With auto-save off a saved batch is still stored, and the queued save is flushed on request.
	/// </summary>
	[TestMethod]
	public void SaveBatch_AutoSaveDisabled_StoresBatchAndQueuesSave()
	{
		WithMockAppData(() =>
		{
			BlastMergeAppData.Get().Settings.AutoSaveEnabled = false;
			BatchConfiguration batch = new() { Name = "queued", FilePatterns = ["*.txt"] };

			Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));
			AppDataBatchManager.SaveIfRequired();

			Assert.AreSame(batch, AppDataBatchManager.LoadBatch("queued"));
		});
	}

	/// <summary>
	/// With auto-save off a batch can still be deleted.
	/// </summary>
	[TestMethod]
	public void DeleteBatch_AutoSaveDisabled_RemovesBatch()
	{
		WithMockAppData(() =>
		{
			BlastMergeAppData.Get().Settings.AutoSaveEnabled = false;
			Assert.IsTrue(AppDataBatchManager.SaveBatch(new BatchConfiguration { Name = "doomed", FilePatterns = ["*.cs"] }));

			Assert.IsTrue(AppDataBatchManager.DeleteBatch("doomed"));
			Assert.IsNull(AppDataBatchManager.LoadBatch("doomed"));
			Assert.IsFalse(AppDataBatchManager.DeleteBatch("doomed"));
		});
	}

	/// <summary>
	/// The default batch is created only while there are no batches at all.
	/// </summary>
	[TestMethod]
	public void CreateDefaultBatchIfNoneExist_CreatesOnlyOnce()
	{
		WithMockAppData(() =>
		{
			Assert.IsTrue(AppDataBatchManager.CreateDefaultBatchIfNoneExist());
			Assert.IsNotNull(AppDataBatchManager.LoadBatch(BatchConfiguration.CreateDefault().Name));
			Assert.HasCount(1, AppDataBatchManager.ListBatches());

			Assert.IsFalse(AppDataBatchManager.CreateDefaultBatchIfNoneExist());
			Assert.HasCount(1, AppDataBatchManager.ListBatches());
		});
	}

	/// <summary>
	/// Recording a batch's use makes it the most recent one, whether or not auto-save is on.
	/// </summary>
	/// <param name="autoSave">Whether auto-save is enabled.</param>
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void RecordBatchUsage_SetsMostRecentBatch(bool autoSave)
	{
		WithMockAppData(() =>
		{
			BlastMergeAppData.Get().Settings.AutoSaveEnabled = autoSave;
			Assert.IsNull(AppDataBatchManager.MostRecentBatch);

			DateTime before = DateTime.UtcNow;
			AppDataBatchManager.RecordBatchUsage("first");
			AppDataBatchManager.RecordBatchUsage("second");

			Assert.AreEqual("second", AppDataBatchManager.MostRecentBatch);
			RecentBatchInfo? recent = BlastMergeAppData.Get().RecentBatch;
			Assert.IsNotNull(recent);
			Assert.IsGreaterThanOrEqualTo(before, recent.LastUsed);
		});
	}

	/// <summary>
	/// A missing batch name is refused.
	/// </summary>
	[TestMethod]
	public void RecordBatchUsage_NullName_Throws()
	{
		WithMockAppData(() => Assert.ThrowsExactly<ArgumentNullException>(() => AppDataBatchManager.RecordBatchUsage(null!)));
	}

	/// <summary>
	/// A blank batch name is refused without changing the most recent batch.
	/// </summary>
	/// <param name="name">The blank name.</param>
	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	public void RecordBatchUsage_BlankName_Throws(string name)
	{
		WithMockAppData(() =>
		{
			AppDataBatchManager.RecordBatchUsage("kept");

			ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => AppDataBatchManager.RecordBatchUsage(name));

			Assert.AreEqual("batchName", exception.ParamName);
			Assert.AreEqual("kept", AppDataBatchManager.MostRecentBatch);
		});
	}

	/// <summary>
	/// Runs a test against empty application data saved to a mock file system. The mock is configured
	/// per thread, and test initialization can run on a different thread from the test itself, so it is
	/// configured here, on the thread that runs the test.
	/// </summary>
	/// <remarks>
	/// The shared instance is emptied before and after the test, and saved before the real file system
	/// is restored so that no queued save is left to be flushed to the user's real application data
	/// when the process exits.
	/// </remarks>
	/// <param name="test">The test body.</param>
	private static void WithMockAppData(Action test)
	{
		AppDataStorage.ConfigureForTesting(() => new MockFileSystem());
		ClearState();
		try
		{
			test();
		}
		finally
		{
			ClearState();
			BlastMergeAppData.Get().Save();
			AppDataStorage.ResetFileSystem();
		}
	}

	/// <summary>
	/// Empties the shared application data and restores its default settings.
	/// </summary>
	private static void ClearState() => BlastMergeAppData.ResetForTesting();
}
