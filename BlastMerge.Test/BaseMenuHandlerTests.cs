// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BaseMenuHandler"/>, driven through a minimal derived handler.
/// </summary>
[TestClass]
[DoNotParallelize]
public class BaseMenuHandlerTests : ConsoleTestBase
{
	/// <summary>
	/// A handler without an application service is refused at construction.
	/// </summary>
	[TestMethod]
	public void Constructor_NullApplicationService_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new ProbeMenuHandler(null!, () => { }));

	/// <summary>
	/// The application service passed in is the one the handler exposes.
	/// </summary>
	[TestMethod]
	public void ApplicationService_IsTheOnePassedIn()
	{
		RecordingMenuApplicationService service = new();
		ProbeMenuHandler handler = new(service, () => { });

		Assert.AreSame(service, handler.Service);
	}

	/// <summary>
	/// Entering a menu records it in the navigation history before handling it.
	/// </summary>
	[TestMethod]
	public void Enter_PushesMenuNameThenHandles()
	{
		string? topWhileHandling = null;
		ProbeMenuHandler handler = new(new RecordingMenuApplicationService(), () => topWhileHandling = NavigationHistory.Peek());

		handler.Enter();

		Assert.AreEqual(ProbeMenuHandler.Name, topWhileHandling);
		Assert.AreEqual(1, NavigationHistory.Count);
		Assert.AreEqual(1, handler.HandleCount);
	}

	/// <summary>
	/// Going back removes the current menu and reports whether there is anywhere left to go.
	/// </summary>
	[TestMethod]
	public void GoBack_PopsCurrentMenuAndReportsWhetherHistoryRemains()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push(ProbeMenuHandler.Name);

		Assert.IsTrue(ProbeMenuHandler.CallGoBack());
		Assert.AreEqual("Main Menu", NavigationHistory.Peek());

		Assert.IsFalse(ProbeMenuHandler.CallGoBack());
		Assert.AreEqual(0, NavigationHistory.Count);
	}

	/// <summary>
	/// The back text names the menu below the current one.
	/// </summary>
	[TestMethod]
	public void GetBackMenuText_NamesThePreviousMenu()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push("Compare Files");
		NavigationHistory.Push(ProbeMenuHandler.Name);

		Assert.AreEqual("🔙 Back to Compare Files", ProbeMenuHandler.CallGetBackMenuText());
	}

	/// <summary>
	/// The menu title is written to the console.
	/// </summary>
	[TestMethod]
	public void ShowMenuTitle_WritesTitle()
	{
		ProbeMenuHandler.CallShowMenuTitle("A [bracketed] title");

		StringAssert.Contains(Output, "A [bracketed] title");
	}

	/// <summary>
	/// An error is shown and then waits for a key; without one the wait fails.
	/// </summary>
	[TestMethod]
	public void ShowError_ShowsMessageAndWaitsForKey()
	{
		Assert.ThrowsExactly<InvalidOperationException>(() => ProbeMenuHandler.CallShowError("broken"));
		StringAssert.Contains(Output, "broken");

		PressAnyKey();
		ProbeMenuHandler.CallShowError("broken again");

		StringAssert.Contains(Output, "broken again");
		StringAssert.Contains(Output, "Press any key to continue...");
	}

	/// <summary>
	/// Success and warning messages are written without waiting.
	/// </summary>
	[TestMethod]
	public void ShowSuccessAndWarning_WriteWithoutWaiting()
	{
		ProbeMenuHandler.CallShowSuccess("went well");
		ProbeMenuHandler.CallShowWarning("watch out");

		StringAssert.Contains(Output, "went well");
		StringAssert.Contains(Output, "watch out");
	}

	/// <summary>
	/// Both wait overloads consume one key and show their prompt.
	/// </summary>
	[TestMethod]
	public void WaitForKeyPress_ConsumesOneKeyPerCall()
	{
		PressAnyKey();
		PressAnyKey();

		ProbeMenuHandler.CallWaitForKeyPress();
		ProbeMenuHandler.CallWaitForKeyPress("Hit something");

		StringAssert.Contains(Output, "Press any key to continue...");
		StringAssert.Contains(Output, "Hit something");
		Assert.ThrowsExactly<InvalidOperationException>(ProbeMenuHandler.CallWaitForKeyPress);
	}

	/// <summary>
	/// A minimal handler exposing the protected members of <see cref="BaseMenuHandler"/>.
	/// </summary>
	/// <param name="applicationService">The application service.</param>
	/// <param name="onHandle">What to do when handled.</param>
	private sealed class ProbeMenuHandler(ApplicationService applicationService, Action onHandle) : BaseMenuHandler(applicationService)
	{
		/// <summary>
		/// The name this handler records in the navigation history.
		/// </summary>
		public const string Name = "Probe Menu";

		/// <summary>
		/// Gets how many times the handler was handled.
		/// </summary>
		public int HandleCount { get; private set; }

		/// <summary>
		/// Gets the application service the handler holds.
		/// </summary>
		public ApplicationService Service => ApplicationService;

		/// <inheritdoc/>
		protected override string MenuName => Name;

		/// <inheritdoc/>
		public override void Handle()
		{
			HandleCount++;
			onHandle();
		}

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.GoBack"/>.
		/// </summary>
		/// <returns>Whether there is a menu to go back to.</returns>
		public static bool CallGoBack() => GoBack();

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.GetBackMenuText"/>.
		/// </summary>
		/// <returns>The back menu text.</returns>
		public static string CallGetBackMenuText() => GetBackMenuText();

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.ShowMenuTitle"/>.
		/// </summary>
		/// <param name="title">The title.</param>
		public static void CallShowMenuTitle(string title) => ShowMenuTitle(title);

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.ShowError"/>.
		/// </summary>
		/// <param name="message">The message.</param>
		public static void CallShowError(string message) => ShowError(message);

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.ShowSuccess"/>.
		/// </summary>
		/// <param name="message">The message.</param>
		public static void CallShowSuccess(string message) => ShowSuccess(message);

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.ShowWarning"/>.
		/// </summary>
		/// <param name="message">The message.</param>
		public static void CallShowWarning(string message) => ShowWarning(message);

		/// <summary>
		/// Calls the parameterless <see cref="BaseMenuHandler.WaitForKeyPress()"/>.
		/// </summary>
		public static void CallWaitForKeyPress() => WaitForKeyPress();

		/// <summary>
		/// Calls <see cref="BaseMenuHandler.WaitForKeyPress(string)"/>.
		/// </summary>
		/// <param name="message">The message.</param>
		public static void CallWaitForKeyPress(string message) => WaitForKeyPress(message);
	}
}
