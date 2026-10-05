// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.IO.Abstractions;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

/// <summary>
/// Pins that user-supplied paths and names reach the console as text rather than as Spectre.Console markup.
/// </summary>
/// <remarks>
/// Bracketed folder names are routine in Next.js, SvelteKit and Nuxt route trees (<c>app/[id]/page.tsx</c>)
/// and in versioned folders (<c>[v2]</c>). Interpolated straight into a markup string, Spectre parses
/// <c>[id]</c> as a style tag and throws, which crashed batch runs over web repositories.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class MarkupEscapingTests : IDisposable
{
	private IAnsiConsole _originalConsole = null!;
	private StringWriter _output = null!;
	private string _tempDirectory = null!;

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

		_tempDirectory = Path.Combine(Path.GetTempPath(), $"BlastMerge_Markup_{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDirectory);
	}

	[TestCleanup]
	public void Cleanup()
	{
		AnsiConsole.Console = _originalConsole;
		Dispose();

		if (Directory.Exists(_tempDirectory))
		{
			Directory.Delete(_tempDirectory, recursive: true);
		}
	}

	private string CreateFile(string relativePath, string content)
	{
		string path = Path.Combine(_tempDirectory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_OverBracketedFolders_WithTheConsoleProgressCallback_DoesNotThrow()
	{
		// Arrange
		CreateFile(Path.Combine("app", "[id]", "page.tsx"), "export default 1;\n");
		CreateFile(Path.Combine("web", "[id]", "page.tsx"), "export default 2;\n");
		BatchConfiguration batch = new()
		{
			Name = "web",
			FilePatterns = ["page.tsx"],
		};
		IFileSystem fileSystem = new FileSystem();

		// Act - the progress callback the console app wires up
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			_tempDirectory,
			(f1, f2, existing) => IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
				f1, f2, existing, (block, context, blockNumber) => BlockChoice.UseVersion2, fileSystem),
			_ => { },
			() => true,
			ConsoleApplicationService.ReportBatchProgress,
			fileSystem: fileSystem);

		// Assert
		Assert.IsNotNull(result, "The batch should complete");
		StringAssert.Contains(_output.ToString(), "[id]", "The bracketed folder should be printed literally");
	}

	[TestMethod]
	public void ReportBatchProgress_WithBracketedPath_PrintsThePathLiterally()
	{
		// Act
		ConsoleApplicationService.ReportBatchProgress("Found: app/[id]/page.tsx");

		// Assert
		StringAssert.Contains(_output.ToString(), "Found: app/[id]/page.tsx");
	}

	[TestMethod]
	[DataRow("Could not read /tmp/probe/[v2]/x.txt")]
	[DataRow("[/]")]
	[DataRow("[")]
	public void UIHelperMessages_WithMarkupLikeText_PrintTheTextLiterally(string message)
	{
		// Act
		UIHelper.ShowError(message);
		UIHelper.ShowWarning(message);
		UIHelper.ShowSuccess(message);
		UIHelper.ShowInfo(message);

		// Assert
		string output = _output.ToString();
		int occurrences = output.Split(message).Length - 1;
		Assert.AreEqual(4, occurrences, $"Each helper should print the message literally. Output: {output}");
	}

	/// <inheritdoc/>
	public void Dispose() => _output?.Dispose();
}
