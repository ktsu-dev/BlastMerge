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
/// <see cref="BlastMergeAppData.ResetForTesting"/> clears a field named <c>_instance</c>, which
/// ktsu.AppDataStorage 1.17.2 does not have, so it does nothing and one instance lives for the whole
/// test run. Tests that touch the history or settings therefore empty them explicitly instead.
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
