// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

/// <summary>
/// Manages navigation history for menu systems.
/// </summary>
public static class NavigationHistory
{
	private static readonly Stack<string> _navigationHistory = new();

	/// <summary>
	/// Pushes a new menu name onto the navigation stack.
	/// </summary>
	/// <param name="menuName">The name of the menu being navigated to.</param>
	public static void Push(string menuName)
	{
		ArgumentNullException.ThrowIfNull(menuName);
		_navigationHistory.Push(menuName);
	}

	/// <summary>
	/// Pops the most recent menu from the navigation stack.
	/// </summary>
	/// <returns>The name of the previous menu, or null if the stack is empty.</returns>
	public static string? Pop() => _navigationHistory.Count > 0 ? _navigationHistory.Pop() : null;

	/// <summary>
	/// Peeks at the most recent menu without removing it from the stack.
	/// </summary>
	/// <returns>The name of the previous menu, or null if the stack is empty.</returns>
	public static string? Peek() => _navigationHistory.Count > 0 ? _navigationHistory.Peek() : null;

	/// <summary>
	/// Gets the number of items in the navigation stack.
	/// </summary>
	public static int Count => _navigationHistory.Count;

	/// <summary>
	/// Clears the navigation stack.
	/// </summary>
	public static void Clear() => _navigationHistory.Clear();

	/// <summary>
	/// Gets an appropriate back menu text based on the navigation stack.
	/// </summary>
	/// <returns>A string describing where the back action will go.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "This method performs complex logic and array operations, making it inappropriate for a property")]
	public static string GetBackMenuText()
	{
		if (Count <= 1)
		{
			return "🔙 Back to Main Menu";
		}

		// Look at the menu below the current one (where back will actually go)
		string[] stackArray = [.. _navigationHistory];
		if (stackArray.Length >= 2)
		{
			string previousMenu = stackArray[1]; // Second from top (below current)
			return previousMenu switch
			{
				"Main Menu" => "🔙 Back to Main Menu",
				_ => $"🔙 Back to {previousMenu}"
			};
		}

		return "🔙 Back to Main Menu";
	}

	/// <summary>
	/// Determines if the back action should go to main menu.
	/// </summary>
	/// <returns>True if should go to main menu, false otherwise.</returns>
	public static bool ShouldGoToMainMenu() => Count == 0 || Peek() == "Main Menu";
}
