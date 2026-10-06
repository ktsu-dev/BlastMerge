// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Cli.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the CLI's <see cref="InputState"/> model.
/// </summary>
[TestClass]
public class InputStateTests
{
	/// <summary>
	/// A new state holds the initial input, puts the cursor at its end and has no history selected.
	/// </summary>
	[TestMethod]
	public void Constructor_StartsAtEndOfInputWithNoHistory()
	{
		InputState state = new("hello");

		Assert.AreEqual("hello", state.Input);
		Assert.AreEqual(5, state.CursorPos);
		Assert.AreEqual(-1, state.HistoryIndex);
	}

	/// <summary>
	/// An empty initial input starts the cursor at zero.
	/// </summary>
	[TestMethod]
	public void Constructor_EmptyInput_CursorAtZero()
	{
		InputState state = new(string.Empty);

		Assert.AreEqual(string.Empty, state.Input);
		Assert.AreEqual(0, state.CursorPos);
	}

	/// <summary>
	/// The properties can be updated independently.
	/// </summary>
	[TestMethod]
	public void Properties_AreSettable()
	{
		InputState state = new("abc")
		{
			Input = "abcd",
			CursorPos = 1,
			HistoryIndex = 2,
		};

		Assert.AreEqual("abcd", state.Input);
		Assert.AreEqual(1, state.CursorPos);
		Assert.AreEqual(2, state.HistoryIndex);
	}
}
