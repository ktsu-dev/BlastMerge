// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ktsu.BlastMerge.Core.Models;

/// <summary>
/// Manages batch configurations for BlastMerge
/// </summary>
public static class BatchManager
{
	private static readonly string BatchConfigDirectory = GetBatchConfigDirectory();
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	/// <summary>
	/// Gets the directory where batch configurations are stored
	/// </summary>
	/// <returns>Path to batch configuration directory</returns>
	private static string GetBatchConfigDirectory()
	{
		try
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (!string.IsNullOrEmpty(appDataPath))
			{
				return Path.Combine(appDataPath, "BlastMerge", "Batches");
			}
		}
		catch (Exception ex) when (ex is System.Security.SecurityException or PlatformNotSupportedException)
		{
			// Fall through
		}

		return Path.Combine(".", "blastmerge_batches");
	}

	/// <summary>
	/// Ensures the batch configuration directory exists
	/// </summary>
	private static void EnsureDirectoryExists()
	{
		try
		{
			if (!Directory.Exists(BatchConfigDirectory))
			{
				Directory.CreateDirectory(BatchConfigDirectory);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			throw new InvalidOperationException($"Cannot create batch configuration directory: {BatchConfigDirectory}", ex);
		}
	}

	/// <summary>
	/// Saves a batch configuration to disk
	/// </summary>
	/// <param name="batch">The batch configuration to save</param>
	/// <returns>True if saved successfully, false otherwise</returns>
	public static bool SaveBatch(BatchConfiguration batch)
	{
		ArgumentNullException.ThrowIfNull(batch);

		if (!batch.IsValid())
		{
			return false;
		}

		try
		{
			EnsureDirectoryExists();

			batch.LastModified = DateTime.UtcNow;
			string fileName = GetSafeFileName(batch.Name) + ".json";
			string filePath = Path.Combine(BatchConfigDirectory, fileName);

			string json = JsonSerializer.Serialize(batch, JsonOptions);
			File.WriteAllText(filePath, json);

			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			return false;
		}
	}

	/// <summary>
	/// Loads a batch configuration by name
	/// </summary>
	/// <param name="name">The name of the batch to load</param>
	/// <returns>The batch configuration, or null if not found</returns>
	public static BatchConfiguration? LoadBatch(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		try
		{
			string fileName = GetSafeFileName(name) + ".json";
			string filePath = Path.Combine(BatchConfigDirectory, fileName);

			if (!File.Exists(filePath))
			{
				return null;
			}

			string json = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<BatchConfiguration>(json, JsonOptions);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			return null;
		}
	}

	/// <summary>
	/// Lists all available batch configurations
	/// </summary>
	/// <returns>A list of batch configuration names</returns>
	public static IReadOnlyCollection<string> ListBatches()
	{
		try
		{
			if (!Directory.Exists(BatchConfigDirectory))
			{
				return [];
			}

			List<string> batchNames = [];
			string[] files = Directory.GetFiles(BatchConfigDirectory, "*.json");

			foreach (string file in files)
			{
				try
				{
					string json = File.ReadAllText(file);
					BatchConfiguration? batch = JsonSerializer.Deserialize<BatchConfiguration>(json, JsonOptions);
					if (batch?.IsValid() == true)
					{
						batchNames.Add(batch.Name);
					}
				}
				catch (JsonException)
				{
					// Skip invalid JSON files
				}
			}

			return batchNames.AsReadOnly();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return [];
		}
	}

	/// <summary>
	/// Deletes a batch configuration
	/// </summary>
	/// <param name="name">The name of the batch to delete</param>
	/// <returns>True if deleted successfully, false otherwise</returns>
	public static bool DeleteBatch(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		try
		{
			string fileName = GetSafeFileName(name) + ".json";
			string filePath = Path.Combine(BatchConfigDirectory, fileName);

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
				return true;
			}

			return false;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets all batch configurations with their details
	/// </summary>
	/// <returns>A list of all batch configurations</returns>
	public static IReadOnlyCollection<BatchConfiguration> GetAllBatches()
	{
		try
		{
			if (!Directory.Exists(BatchConfigDirectory))
			{
				return [];
			}

			List<BatchConfiguration> batches = [];
			string[] files = Directory.GetFiles(BatchConfigDirectory, "*.json");

			foreach (string file in files)
			{
				try
				{
					string json = File.ReadAllText(file);
					BatchConfiguration? batch = JsonSerializer.Deserialize<BatchConfiguration>(json, JsonOptions);
					if (batch?.IsValid() == true)
					{
						batches.Add(batch);
					}
				}
				catch (JsonException)
				{
					// Skip invalid JSON files
				}
			}

			return batches.OrderBy(b => b.Name).ToList().AsReadOnly();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return [];
		}
	}

	/// <summary>
	/// Creates a safe filename from a batch name
	/// </summary>
	/// <param name="name">The batch name</param>
	/// <returns>A safe filename</returns>
	private static string GetSafeFileName(string name)
	{
		string safeName = name;
		char[] invalidChars = Path.GetInvalidFileNameChars();

		foreach (char c in invalidChars)
		{
			safeName = safeName.Replace(c, '_');
		}

		return safeName;
	}

	/// <summary>
	/// Creates and saves a default batch configuration if none exist
	/// </summary>
	/// <returns>True if default was created, false if batches already exist</returns>
	public static bool CreateDefaultBatchIfNoneExist()
	{
		if (ListBatches().Count == 0)
		{
			BatchConfiguration defaultBatch = BatchConfiguration.CreateDefault();
			return SaveBatch(defaultBatch);
		}
		return false;
	}
}
