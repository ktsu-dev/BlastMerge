// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BinaryContentException"/>.
/// </summary>
[TestClass]
public class BinaryContentExceptionTests
{
	/// <summary>
	/// The parameterless constructor explains that binary content cannot be merged as text.
	/// </summary>
	[TestMethod]
	public void DefaultConstructor_HasExplanatoryMessage()
	{
		BinaryContentException exception = new();

		Assert.AreEqual("Binary content cannot be merged as text.", exception.Message);
		Assert.IsNull(exception.InnerException);
	}

	/// <summary>
	/// The message constructor keeps the message as given.
	/// </summary>
	[TestMethod]
	public void MessageConstructor_KeepsMessage()
	{
		BinaryContentException exception = new("custom");

		Assert.AreEqual("custom", exception.Message);
	}

	/// <summary>
	/// The message and inner-exception constructor keeps both.
	/// </summary>
	[TestMethod]
	public void InnerExceptionConstructor_KeepsMessageAndInner()
	{
		InvalidCastException inner = new("cause");

		BinaryContentException exception = new("outer", inner);

		Assert.AreEqual("outer", exception.Message);
		Assert.AreSame(inner, exception.InnerException);
	}

	/// <summary>
	/// The factory names both files, and the exception is an <see cref="InvalidOperationException"/>.
	/// </summary>
	[TestMethod]
	public void ForFiles_NamesBothFiles()
	{
		BinaryContentException exception = BinaryContentException.ForFiles("first.bin", "second.bin");

		Assert.Contains("'first.bin'", exception.Message);
		Assert.Contains("'second.bin'", exception.Message);
		Assert.IsInstanceOfType<InvalidOperationException>(exception);
	}
}
