// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Cli.Models;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="IterativeMergeMenuHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class IterativeMergeMenuHandlerTests : ConsoleTestBase
{
	private RecordingMenuApplicationService service = null!;
	private IterativeMergeMenuHandler handler = null!;

	/// <summary>
	/// Creates the handler under test, entered from the main menu, over empty application data.
	/// </summary>
	/// <remarks>
	/// The application data is one instance shared by every test, so history left by an earlier
	/// test would otherwise still be there.
	/// </remarks>
	[TestInitialize]
	public void CreateHandler()
	{
		SharedAppDataState.Reset();
		service = new RecordingMenuApplicationService();
		handler = new IterativeMergeMenuHandler(service);
		NavigationHistory.Push("Main Menu");
	}

	/// <summary>
	/// Leaves the shared application data empty for whichever test runs next.
	/// </summary>
	[TestCleanup]
	public void RestoreAppData() => SharedAppDataState.Reset();

	/// <summary>
	/// The entered directory and pattern are merged, then the menu waits and returns.
	/// </summary>
	[TestMethod]
	public void Handle_RunsMergeThenGoesBack()
	{
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("*.cs");
		PressAnyKey();

		handler.Enter();

		StringAssert.Contains(Output, "Run Iterative Merge");
		Assert.AreSequenceEqual([$"RunIterativeMerge({TempDirectory}, *.cs)"], service.Calls);
		Assert.AreEqual(1, NavigationHistory.Count);
		Assert.AreEqual("Main Menu", NavigationHistory.Peek());
	}

	/// <summary>
	/// The merge does not return until a key is pressed.
	/// </summary>
	[TestMethod]
	public void Handle_WaitsForKeyAfterMerge()
	{
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("*.cs");

		Assert.ThrowsExactly<InvalidOperationException>(handler.Handle);
		Assert.HasCount(1, service.Calls);
	}

	/// <summary>
	/// The entered values are remembered for next time.
	/// </summary>
	[TestMethod]
	public void Handle_RecordsInputHistory()
	{
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("*.cs");
		PressAnyKey();

		handler.Handle();

		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("directory"));
		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("filename"));
	}

	/// <summary>
	/// An empty directory cancels without merging.
	/// </summary>
	[TestMethod]
	public void Handle_EmptyDirectory_Cancels()
	{
		Input.PushKey(ConsoleKey.Enter);

		handler.Enter();

		Assert.IsEmpty(service.Calls);
		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("filename pattern", Output);
	}

	/// <summary>
	/// An empty pattern cancels without merging.
	/// </summary>
	[TestMethod]
	public void Handle_EmptyPattern_Cancels()
	{
		Input.PushTextWithEnter(TempDirectory);
		Input.PushKey(ConsoleKey.Enter);

		handler.Enter();

		Assert.IsEmpty(service.Calls);
		StringAssert.Contains(Output, "Operation cancelled.");
	}
}
