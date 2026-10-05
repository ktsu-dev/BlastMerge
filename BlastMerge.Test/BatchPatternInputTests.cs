// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

/// <summary>
/// Pins how the batch editor accepts the file patterns a user types.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BatchPatternInputTests : IDisposable
{
	private IAnsiConsole _originalConsole = null!;
	private StringWriter _output = null!;

	[TestInitialize]
	public void Setup()
	{
		_originalConsole = AnsiConsole.Console;
		_output = new StringWriter();
		IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.No,
			ColorSystem = ColorSystemSupport.NoColors,
			Interactive = InteractionSupport.No,
			Out = new AnsiConsoleOutput(_output),
		});

		// A StringWriter reports no width, and Spectre renders nothing into a zero-width console
		console.Profile.Width = 1000;
		AnsiConsole.Console = console;
	}

	[TestCleanup]
	public void Cleanup()
	{
		AnsiConsole.Console = _originalConsole;
		Dispose();
	}

	/// <inheritdoc/>
	public void Dispose() => _output?.Dispose();

	[TestMethod]
	[DataRow("*.cs", "*.cs")]
	[DataRow("  .gitignore  ", ".gitignore")]
	[DataRow("**/*.cs", "*.cs")]
	public void TryAddPattern_WithAFileNamePattern_AddsItNormalised(string input, string expected)
	{
		// Arrange
		List<string> patterns = [];

		// Act
		bool added = BatchOperationsMenuHandler.TryAddPattern(patterns, input);

		// Assert
		Assert.IsTrue(added, "A file-name pattern should be accepted");
		CollectionAssert.AreEqual(new[] { expected }, patterns);
		Assert.AreEqual(string.Empty, _output.ToString(), "Nothing should be reported for an accepted pattern");
	}

	[TestMethod]
	[DataRow("src/*.cs")]
	[DataRow("**/src/*.cs")]
	public void TryAddPattern_WithAPatternThatNamesAFolder_RejectsItWithAMessage(string input)
	{
		// Arrange
		List<string> patterns = [];

		// Act
		bool added = BatchOperationsMenuHandler.TryAddPattern(patterns, input);

		// Assert
		Assert.IsFalse(added, "A pattern that names a folder can never match a file name");
		Assert.IsEmpty(patterns, "The rejected pattern should not be added");
		StringAssert.Contains(_output.ToString(), "cannot contain a directory separator");
	}
}
