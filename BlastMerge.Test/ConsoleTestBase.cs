// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Testing;
using AppDataStorage = ktsu.AppDataStorage.AppData;

/// <summary>
/// Base class for tests that drive the console application through Spectre.Console.
/// </summary>
/// <remarks>
/// <para>
/// Installs an interactive <see cref="TestConsole"/> as <see cref="AnsiConsole.Console"/>, so prompts
/// read their answers from <see cref="Input"/> and everything written is captured in
/// <see cref="Output"/>. A prompt with no queued input throws rather than blocking, so a test that
/// under-supplies keystrokes fails instead of hanging.
/// </para>
/// <para>
/// Application data is redirected to an in-memory file system, so history, settings and batch
/// configurations written by the code under test never reach the real user profile. Navigation
/// history is static and is cleared on both sides of each test.
/// </para>
/// <para>
/// Derived classes must be marked <c>[DoNotParallelize]</c>: the console, the application data
/// singleton and the navigation history are all process-wide.
/// </para>
/// </remarks>
public abstract class ConsoleTestBase
{
	private IAnsiConsole originalConsole = null!;
	private string originalCurrentDirectory = string.Empty;

	/// <summary>
	/// Gets the console the code under test writes to and reads from.
	/// </summary>
	protected TestConsole Console { get; private set; } = null!;

	/// <summary>
	/// Gets the queue of keystrokes prompts will read.
	/// </summary>
	protected TestConsoleInput Input => Console.Input;

	/// <summary>
	/// Gets everything written to the console so far.
	/// </summary>
	protected string Output => Console.Output;

	/// <summary>
	/// Gets a real temporary directory, created per test and deleted afterwards.
	/// </summary>
	protected string TempDirectory { get; private set; } = string.Empty;

	/// <summary>
	/// Installs the test console and isolated application data.
	/// </summary>
	[TestInitialize]
	public void ConsoleTestSetup()
	{
		originalConsole = AnsiConsole.Console;
		Console = new TestConsole();
		Console.Profile.Capabilities.Interactive = true;
		Console.Profile.Width = 200;
		AnsiConsole.Console = Console;

		TempDirectory = SecureTempFileHelper.CreateTempDirectory();

		// ktsu.AppDataStorage rejects a storage directory name that already exists as a file
		// relative to the working directory, and the Unix test apphost is exactly that file.
		originalCurrentDirectory = Environment.CurrentDirectory;
		Environment.CurrentDirectory = TempDirectory;

		AppDataStorage.ConfigureForTesting(() => new MockFileSystem());
		BlastMergeAppData.ResetForTesting();
		NavigationHistory.Clear();
	}

	/// <summary>
	/// Restores the real console and application data.
	/// </summary>
	[TestCleanup]
	public void ConsoleTestCleanup()
	{
		NavigationHistory.Clear();

		FlushQueuedSave();
		BlastMergeAppData.ResetForTesting();
		AppDataStorage.ResetFileSystem();

		AnsiConsole.Console = originalConsole;
		Environment.CurrentDirectory = originalCurrentDirectory;
		SecureTempFileHelper.SafeDeleteTempDirectory(TempDirectory);
		Console.Dispose();
	}

	/// <summary>
	/// Saves the application data to the in-memory file system before it is removed.
	/// </summary>
	/// <remarks>
	/// A save the code under test queued is otherwise flushed at process exit, on a thread that never
	/// saw the in-memory file system, and lands in the real user profile.
	/// </remarks>
	private static void FlushQueuedSave()
	{
		try
		{
			BlastMergeAppData.Get().Save();
		}
		catch (IOException)
		{
			// The test broke the in-memory file system on purpose to exercise a failed save, so
			// there is nothing to flush and nowhere it could go.
		}
	}

	/// <summary>
	/// Writes a file under <see cref="TempDirectory"/> and returns its full path.
	/// </summary>
	/// <param name="relativePath">The path relative to the temporary directory.</param>
	/// <param name="content">The file content.</param>
	/// <returns>The full path of the written file.</returns>
	protected string WriteFile(string relativePath, string content)
	{
		string path = Path.Combine(TempDirectory, relativePath);
		string? directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(path, content);
		return path;
	}

	/// <summary>
	/// Queues a selection of the item <paramref name="index"/> places below the first in a selection prompt.
	/// </summary>
	/// <param name="index">The zero-based index of the choice to select.</param>
	protected void SelectIndex(int index)
	{
		for (int i = 0; i < index; i++)
		{
			Input.PushKey(ConsoleKey.DownArrow);
		}

		Input.PushKey(ConsoleKey.Enter);
	}

	/// <summary>
	/// Queues a key press for code waiting on "press any key".
	/// </summary>
	protected void PressAnyKey() => Input.PushKey(ConsoleKey.Enter);
}
