// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// Tracks the most recently used batch configuration for quick access.
/// </summary>
public static class RecentBatchTracker
{
	private static readonly string RecentBatchFile = GetRecentBatchFilePath();
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	/// <summary>
	/// Gets the recent batch file path, with fallback to temp directory if ApplicationData is not accessible.
	/// </summary>
	/// <returns>Path to recent batch file.</returns>
	private static string GetRecentBatchFilePath()
	{
		try
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (!string.IsNullOrEmpty(appDataPath))
			{
				return Path.Combine(appDataPath, "BlastMerge", "recent_batch.json");
			}
		}
		catch (Exception e) when (e is System.Security.SecurityException or PlatformNotSupportedException)
		{
			// Fall through
		}

		return Path.Combine(".", "blastmerge_recent_batch.json");
	}

	/// <summary>
	/// Records that a batch configuration was used.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration that was used.</param>
	public static void RecordBatchUsage(string batchName)
	{
		ArgumentNullException.ThrowIfNull(batchName);

		try
		{
			RecentBatchInfo recentInfo = new()
			{
				BatchName = batchName,
				LastUsed = DateTime.UtcNow
			};

			string? directory = Path.GetDirectoryName(RecentBatchFile);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string json = JsonSerializer.Serialize(recentInfo, JsonOptions);
			File.WriteAllText(RecentBatchFile, json);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or System.Security.SecurityException)
		{
			// Ignore failures - recent batch tracking is not critical
		}
	}

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <returns>The name of the most recent batch, or null if none found.</returns>
	public static string? GetMostRecentBatch()
	{
		try
		{
			if (!File.Exists(RecentBatchFile))
			{
				return null;
			}

			string json = File.ReadAllText(RecentBatchFile);
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			RecentBatchInfo? recentInfo = JsonSerializer.Deserialize<RecentBatchInfo>(json);
			return recentInfo?.BatchName;
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or System.Security.SecurityException)
		{
			// If any error occurs, just return null
			return null;
		}
	}

	/// <summary>
	/// Represents information about the most recently used batch.
	/// </summary>
	private sealed class RecentBatchInfo
	{
		/// <summary>
		/// Gets or sets the name of the most recent batch.
		/// </summary>
		public string BatchName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets when the batch was last used.
		/// </summary>
		public DateTime LastUsed { get; set; }
	}
}
