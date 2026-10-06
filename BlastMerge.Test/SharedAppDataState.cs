// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using ktsu.BlastMerge.Models;
using AppDataStorage = ktsu.AppDataStorage.AppData;

/// <summary>
/// Puts the shared application data into a known state for tests that read or write input history.
/// </summary>
/// <remarks>
/// <see cref="ConsoleTestBase"/> already discards the instance before each test. These helpers are for
/// tests that swap in a file system they can read, where the instance must be emptied in place so the
/// first load does not write a file the test then mistakes for its own save.
/// </remarks>
internal static class SharedAppDataState
{
	/// <summary>
	/// The file the application data is saved to.
	/// </summary>
	private const string SaveFileName = "blast_merge_app_data.json";

	/// <summary>
	/// Empties the input history and restores the default settings.
	/// </summary>
	public static void Reset()
	{
		BlastMergeAppData appData = BlastMergeAppData.Get();
		appData.InputHistory.Clear();
		appData.Settings = new();
	}

	/// <summary>
	/// Points application data storage at a new in-memory file system the caller can inspect.
	/// </summary>
	/// <returns>The file system saves will now be written to.</returns>
	public static MockFileSystem UseInspectableFileSystem()
	{
		MockFileSystem fileSystem = new();
		AppDataStorage.ResetFileSystem();
		AppDataStorage.ConfigureForTesting(() => fileSystem);
		return fileSystem;
	}

	/// <summary>
	/// Reads the saved application data from a file system, if it has been saved.
	/// </summary>
	/// <param name="fileSystem">The file system to look in.</param>
	/// <returns>The saved text, or <see langword="null"/> when nothing has been saved.</returns>
	public static string? ReadSaved(MockFileSystem fileSystem)
	{
		string? path = fileSystem.AllFiles.FirstOrDefault(f => fileSystem.Path.GetFileName(f) == SaveFileName);
		return path is null ? null : fileSystem.File.ReadAllText(path);
	}
}
