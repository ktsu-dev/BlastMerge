// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="ColorCodePair"/>.
/// </summary>
[TestClass]
public class ColorCodePairTests
{
	/// <summary>
	/// The empty pair adds nothing around text.
	/// </summary>
	[TestMethod]
	public void None_HasEmptyPrefixAndSuffix()
	{
		ColorCodePair pair = ColorCodePair.None;

		Assert.AreEqual(string.Empty, pair.Prefix);
		Assert.AreEqual(string.Empty, pair.Suffix);
	}

	/// <summary>
	/// An ANSI pair resets with the standard reset code unless another is given.
	/// </summary>
	[TestMethod]
	public void CreateAnsi_UsesStandardResetByDefault()
	{
		ColorCodePair pair = ColorCodePair.CreateAnsi("\u001b[32m");

		Assert.AreEqual("\u001b[32m", pair.Prefix);
		Assert.AreEqual("\u001b[0m", pair.Suffix);
	}

	/// <summary>
	/// A custom reset code replaces the default.
	/// </summary>
	[TestMethod]
	public void CreateAnsi_AcceptsCustomReset()
	{
		ColorCodePair pair = ColorCodePair.CreateAnsi("\u001b[31m", "\u001b[39m");

		Assert.AreEqual("\u001b[39m", pair.Suffix);
		Assert.AreEqual(new ColorCodePair { Prefix = "\u001b[31m", Suffix = "\u001b[39m" }, pair);
	}
}
