// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Text;

/// <summary>
/// Parameters for pattern processing operations
/// </summary>
/// <param name="Pattern">The file pattern to process</param>
/// <param name="SearchPaths">The search paths to use</param>
/// <param name="Directory">The default directory to search in</param>
/// <param name="PathExclusionPatterns">Path exclusion patterns to apply</param>
/// <param name="FileSystem">File system abstraction to use (optional, defaults to real filesystem)</param>
public record PatternProcessingParameters(
	string Pattern,
	IReadOnlyCollection<string> SearchPaths,
	string Directory,
	IReadOnlyCollection<string> PathExclusionPatterns,
	IFileSystem? FileSystem = null);

/// <summary>
/// Callback functions for batch processing operations
/// </summary>
/// <param name="MergeCallback">Callback function to perform individual merges</param>
/// <param name="StatusCallback">Callback function to report merge status</param>
/// <param name="ContinuationCallback">Callback function to ask whether to continue</param>
/// <param name="ProgressCallback">Optional callback to report progress updates</param>
public record ProcessingCallbacks(
	Func<string, string, string?, MergeResult?> MergeCallback,
	Action<MergeSessionStatus> StatusCallback,
	Func<bool> ContinuationCallback,
	Action<string>? ProgressCallback = null);

/// <summary>
/// The outcome of the hashing phase: the hashes that were computed, and the files that could not be read.
/// </summary>
/// <param name="FileHashes">Map of file path to computed hash, for every file that was read successfully.</param>
/// <param name="Failures">The files that could not be read, and were therefore left out of <paramref name="FileHashes"/>.</param>
internal sealed record HashingPhaseResult(
	IReadOnlyDictionary<string, string> FileHashes,
	IReadOnlyCollection<FileHashFailure> Failures);

/// <summary>
/// Processes batch operations for multiple file patterns
/// </summary>
public static partial class BatchProcessor
{
	private const string OnlyOneFileFoundMessage = "Only one file found, no merge needed";
	private const string AllFilesIdenticalMessage = "All files are identical";
	private const string NoFilesFoundMessage = "No files found";
	private const string MergeCompletedSuccessfullyMessage = "Merge completed successfully";
	private const string AllFilesBinaryMessage = "All files are binary, so none were merged";

	/// <summary>
	/// Processes a batch configuration with search paths and exclusion patterns
	/// </summary>
	/// <param name="batch">The batch configuration to process</param>
	/// <param name="directory">The default directory to search in (used if batch.SearchPaths is empty)</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <param name="patternCallback">Optional callback for each pattern before processing</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The batch processing result</returns>
	public static BatchResult ProcessBatch(
		BatchConfiguration batch,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		Func<string, bool>? patternCallback = null,
		IFileSystem? fileSystem = null)
	{
		Ensure.NotNull(batch);
		Ensure.NotNull(directory);
		Ensure.NotNull(mergeCallback);
		Ensure.NotNull(statusCallback);
		Ensure.NotNull(continuationCallback);

		fileSystem ??= new FileSystem();

		BatchResult result = new()
		{
			BatchName = batch.Name,
			Success = true
		};

		if (!fileSystem.Directory.Exists(directory))
		{
			result.Success = false;
			result.Summary = $"Directory does not exist: {directory}";
			return result;
		}

		foreach (string pattern in batch.FilePatterns)
		{
			// Check if user wants to skip this pattern (if callback provided)
			if (batch.PromptBeforeEachPattern && patternCallback != null && !patternCallback(pattern))
			{
				PatternResult skippedResult = new()
				{
					Pattern = pattern,
					Success = true,
					Message = "Skipped by user"
				};
				result.PatternResults.Add(skippedResult);
				continue;
			}

			PatternResult patternResult = ProcessSinglePatternWithPaths(
				pattern,
				batch.SearchPaths,
				directory,
				batch.PathExclusionPatterns,
				mergeCallback,
				statusCallback,
				continuationCallback,
				fileSystem);

			result.PatternResults.Add(patternResult);

			// Skip empty patterns if configured to do so
			if (!patternResult.Success && batch.SkipEmptyPatterns && patternResult.FilesFound == 0)
			{
				patternResult.Success = true; // Don't count as failure if we're skipping empty patterns
				patternResult.Message = "No files found (skipped)";
			}

			// Update overall batch success
			if (!patternResult.Success)
			{
				result.Success = false;
			}
		}

		// Generate summary
		result.TotalPatternsProcessed = result.PatternResults.Count;
		result.SuccessfulPatterns = result.PatternResults.Count(r => r.Success);

		if (result.Success)
		{
			result.Summary = $"Batch completed successfully. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
		}
		else
		{
			int failedPatterns = result.TotalPatternsProcessed - result.SuccessfulPatterns;
			result.Summary = $"Batch completed with {failedPatterns} failed patterns. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
		}

		return result;
	}

	/// <summary>
	/// Processes a batch using discrete phases: gathering, hashing, grouping, and resolving.
	/// User interaction only occurs in the resolving phase.
	/// </summary>
	/// <param name="batch">The batch configuration.</param>
	/// <param name="directory">The default directory to search in.</param>
	/// <param name="mergeCallback">Callback function to perform individual merges.</param>
	/// <param name="statusCallback">Callback function to report merge status.</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue.</param>
	/// <param name="progressCallback">Optional callback for progress updates during phases.</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent hashing operations (0 for auto).</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem).</param>
	/// <returns>The batch processing result.</returns>
	public static BatchResult ProcessBatchWithDiscretePhases(
		BatchConfiguration batch,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		Action<string>? progressCallback = null,
		int maxDegreeOfParallelism = 0,
		IFileSystem? fileSystem = null)
	{
		Ensure.NotNull(batch);
		Ensure.NotNull(directory);
		Ensure.NotNull(mergeCallback);
		Ensure.NotNull(statusCallback);
		Ensure.NotNull(continuationCallback);

		fileSystem ??= new FileSystem();

		BatchResult result = new()
		{
			BatchName = batch.Name,
			Success = true
		};

		if (!fileSystem.Directory.Exists(directory))
		{
			result.Success = false;
			result.Summary = $"Directory does not exist: {directory}";
			return result;
		}

		try
		{
			// Phase 1: Gathering - Discover all files for all patterns
			progressCallback?.Invoke(ProgressMessages.Phase1GatheringFiles);
			Dictionary<string, IReadOnlyCollection<string>> patternFiles = ExecuteGatheringPhase(batch, directory, progressCallback, fileSystem);

			int totalFiles = patternFiles.Values.Sum(files => files.Count);
			progressCallback?.Invoke($"✅ Gathering complete: Found {totalFiles} files across {batch.FilePatterns.Count} patterns");
			progressCallback?.Invoke("");

			if (totalFiles == 0)
			{
				result.Summary = "No files found for any patterns";
				return result;
			}

			// Phase 2: Hashing - Compute hashes for all files in parallel
			progressCallback?.Invoke("🔗 PHASE 2: Computing file hashes...");
			HashingPhaseResult hashingResult = ExecuteHashingPhase(patternFiles, maxDegreeOfParallelism, progressCallback, fileSystem);

			progressCallback?.Invoke($"✅ Hashing complete: Computed hashes for {hashingResult.FileHashes.Count} files");

			if (hashingResult.Failures.Count > 0)
			{
				progressCallback?.Invoke($"⚠️ {hashingResult.Failures.Count} file(s) could not be read and were skipped");
			}

			progressCallback?.Invoke("");

			// Phase 3: Grouping - Group files by filename and hash
			progressCallback?.Invoke(ProgressMessages.Phase3GroupingFiles);
			List<ResolutionItem> resolutionQueue = ExecuteGroupingPhase(patternFiles, hashingResult, batch.FilePatterns, progressCallback);

			progressCallback?.Invoke($"✅ Grouping complete: Created {resolutionQueue.Count} resolution items");
			progressCallback?.Invoke("");

			// Phase 4: Resolving - Process resolution queue with user interaction
			progressCallback?.Invoke(ProgressMessages.Phase4ResolvingConflicts);
			ExecuteResolvingPhase(resolutionQueue, mergeCallback, statusCallback, continuationCallback, progressCallback, result);

			progressCallback?.Invoke("✅ Resolving complete");

			// Generate final summary
			result.TotalPatternsProcessed = result.PatternResults.Count;
			result.SuccessfulPatterns = result.PatternResults.Count(r => r.Success);

			if (result.Success)
			{
				result.Summary = $"Batch completed successfully. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
			}
			else
			{
				int failedPatterns = result.TotalPatternsProcessed - result.SuccessfulPatterns;
				result.Summary = $"Batch completed with {failedPatterns} failed patterns. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			result.Success = false;
			result.Summary = $"Error during batch processing: {ex.Message}";
		}

		return result;
	}

	/// <summary>
	/// Phase 1: Gathering - Discovers all files for all patterns in parallel.
	/// </summary>
	private static Dictionary<string, IReadOnlyCollection<string>> ExecuteGatheringPhase(
		BatchConfiguration batch,
		string directory,
		Action<string>? progressCallback,
		IFileSystem fileSystem)
	{
		progressCallback?.Invoke($"📂 Scanning {batch.FilePatterns.Count} patterns across search paths...");

		// Create a wrapper for the progress callback to show file paths as they're discovered
		Action<string>? fileDiscoveryCallback = progressCallback != null
			? filePath => progressCallback($"  📄 Found: {filePath}")
			: null;

		Dictionary<string, IReadOnlyCollection<string>> result = GatherAllPatternFiles(batch, directory, fileDiscoveryCallback, fileSystem);

		foreach ((string pattern, IReadOnlyCollection<string> files) in result)
		{
			progressCallback?.Invoke($"  • Pattern '{pattern}': {files.Count} files");
		}

		return result;
	}

	/// <summary>
	/// Phase 2: Hashing - Computes hashes for all files in parallel.
	/// </summary>
	private static HashingPhaseResult ExecuteHashingPhase(
		Dictionary<string, IReadOnlyCollection<string>> patternFiles,
		int maxDegreeOfParallelism,
		Action<string>? progressCallback,
		IFileSystem fileSystem)
	{
		// Flatten all files and prepare for hashing
		List<(string filePath, string fileName)> workItems = [.. patternFiles
			.SelectMany(kvp => kvp.Value.Select(filePath => (filePath, Path.GetFileName(filePath))))
			.Distinct()];

		progressCallback?.Invoke($"⚡ Starting hash computation for {workItems.Count} files...");

		return HashFilesInParallel(workItems, maxDegreeOfParallelism, progressCallback, fileSystem);
	}

	/// <summary>
	/// Phase 3: Grouping - Groups files by filename and hash, creating resolution items.
	/// </summary>
	private static List<ResolutionItem> ExecuteGroupingPhase(
		Dictionary<string, IReadOnlyCollection<string>> patternFiles,
		HashingPhaseResult hashingResult,
		IReadOnlyCollection<string> patterns,
		Action<string>? progressCallback)
	{
		IReadOnlyDictionary<string, string> fileHashes = hashingResult.FileHashes;

		// The same path can be discovered by more than one pattern, so index the failures by path
		Dictionary<string, FileHashFailure> failuresByPath = hashingResult.Failures
			.GroupBy(failure => failure.FilePath)
			.ToDictionary(group => group.Key, group => group.First());

		// Flatten all files and group by filename
		IEnumerable<string> allFiles = patternFiles.Values.SelectMany(files => files);
		IEnumerable<IGrouping<string, string>> fileNameGroups = allFiles.GroupBy(filePath => Path.GetFileName(filePath));

		List<ResolutionItem> resolutionQueue = [];

		foreach (IGrouping<string, string> fileNameGroup in fileNameGroups)
		{
			string fileName = fileNameGroup.Key;
			progressCallback?.Invoke($"  • Analyzing '{fileName}'...");

			// Group files by hash within this filename
			List<IGrouping<string, string>> hashGroups = [.. fileNameGroup
				.Where(fileHashes.ContainsKey)
				.GroupBy(filePath => fileHashes[filePath])];

			// Create FileGroup objects
			List<FileGroup> fileGroups = [.. hashGroups.Select(hashGroup =>
				new FileGroup([.. hashGroup]) { Hash = hashGroup.Key }
			)];

			// Carry the files that could not be read alongside the groups, so the resolving phase
			// can report them instead of treating the smaller group as the whole story
			List<FileHashFailure> skippedFiles = [.. fileNameGroup
				.Distinct()
				.Where(failuresByPath.ContainsKey)
				.Select(filePath => failuresByPath[filePath])];

			// Determine resolution type and create resolution item
			string matchingPattern = FindMatchingPattern(fileName, patterns);
			ResolutionType resolutionType = DetermineResolutionType(fileGroups);

			ResolutionItem resolutionItem = new()
			{
				Pattern = matchingPattern,
				FileName = fileName,
				FileGroups = fileGroups.AsReadOnly(),
				ResolutionType = resolutionType,
				SkippedFiles = skippedFiles.AsReadOnly()
			};

			resolutionQueue.Add(resolutionItem);

			progressCallback?.Invoke($"    → {resolutionType}: {resolutionItem.TotalFiles} files, {resolutionItem.UniqueVersions} versions");

			if (skippedFiles.Count > 0)
			{
				progressCallback?.Invoke($"    ⚠️ {skippedFiles.Count} file(s) skipped: {string.Join(", ", skippedFiles.Select(f => f.FilePath))}");
			}
		}

		return resolutionQueue;
	}

	/// <summary>
	/// Phase 4: Resolving - Processes the resolution queue with user interaction.
	/// </summary>
	private static void ExecuteResolvingPhase(
		List<ResolutionItem> resolutionQueue,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		Action<string>? progressCallback,
		BatchResult result)
	{
		int processedItems = 0;
		int totalItems = resolutionQueue.Count;

		foreach (ResolutionItem resolutionItem in resolutionQueue)
		{
			processedItems++;
			progressCallback?.Invoke($"📋 Processing item {processedItems}/{totalItems}: '{resolutionItem.FileName}' ({resolutionItem.ResolutionType})");

			PatternResult patternResult = ProcessResolutionItem(
				resolutionItem,
				mergeCallback,
				statusCallback,
				continuationCallback);

			result.PatternResults.Add(patternResult);

			if (!patternResult.Success)
			{
				result.Success = false;
			}
		}
	}

	/// <summary>
	/// Determines the type of resolution needed based on the file groups.
	/// </summary>
	private static ResolutionType DetermineResolutionType(List<FileGroup> fileGroups)
	{
		int totalFiles = fileGroups.Sum(g => g.FilePaths.Count);

		return totalFiles switch
		{
			0 => ResolutionType.Empty,
			1 => ResolutionType.SingleFile,
			_ when fileGroups.Count == 1 => ResolutionType.Identical,
			_ => ResolutionType.Merge
		};
	}

	/// <summary>
	/// Processes a single resolution item, handling user interaction only for merge items.
	/// </summary>
	private static PatternResult ProcessResolutionItem(
		ResolutionItem resolutionItem,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback)
	{
		PatternResult result = new()
		{
			Pattern = resolutionItem.Pattern,
			FileName = resolutionItem.FileName,
			FilesFound = resolutionItem.TotalFiles,
			UniqueVersions = resolutionItem.UniqueVersions,
			Success = true
		};

		switch (resolutionItem.ResolutionType)
		{
			case ResolutionType.Empty:
				result.Message = NoFilesFoundMessage;
				break;

			case ResolutionType.SingleFile:
				result.Message = "Only one file found, no action needed";
				break;

			case ResolutionType.Identical:
				result.Message = "All files identical - skipped";
				break;

			case ResolutionType.Merge:
				// This is the only case that requires user interaction
				MergeCompletionResult mergeResult = IterativeMergeOrchestrator.StartIterativeMergeProcess(
					resolutionItem.FileGroups,
					mergeCallback,
					statusCallback,
					continuationCallback);

				result.MergeResult = mergeResult;
				result.Success = mergeResult.IsSuccessful;
				result.Message = mergeResult.IsSuccessful ? MergeCompletedSuccessfullyMessage : $"Merge failed: {mergeResult.OriginalFileName}";
				break;
			default:
				break;
		}

		// Files that could not be read were excluded from the groups above, so the resolution
		// that just ran covers only part of what was found. Report that, rather than letting a
		// shrunken group pass as "no files found" or "only one file found".
		if (resolutionItem.SkippedFiles.Count > 0)
		{
			foreach (FileHashFailure failure in resolutionItem.SkippedFiles)
			{
				result.SkippedFiles.Add(failure);
			}

			result.Success = false;
			result.Message = FormatSkippedFilesMessage(result.Message, resolutionItem.SkippedFiles);
		}

		return result;
	}

	/// <summary>
	/// Builds a message naming every file that could not be read, preserving whatever the
	/// resolution itself had to say.
	/// </summary>
	/// <param name="resolutionMessage">The message produced by resolving the files that were readable.</param>
	/// <param name="skippedFiles">The files that could not be read.</param>
	/// <returns>The combined message.</returns>
	private static string FormatSkippedFilesMessage(string resolutionMessage, IReadOnlyList<FileHashFailure> skippedFiles)
	{
		string fileWord = skippedFiles.Count == 1 ? "file" : "files";
		string details = string.Join("; ", skippedFiles.Select(failure => $"{failure.FilePath} ({failure.ErrorMessage})"));
		string skippedMessage = $"Skipped {skippedFiles.Count} {fileWord} that could not be read: {details}";

		return string.IsNullOrEmpty(resolutionMessage)
			? skippedMessage
			: $"{resolutionMessage}. {skippedMessage}";
	}

	/// <summary>
	/// Gathers all files for all patterns in parallel with progress reporting
	/// </summary>
	/// <param name="batch">The batch configuration</param>
	/// <param name="directory">The default directory to search in</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>Dictionary mapping pattern to found files</returns>
	private static Dictionary<string, IReadOnlyCollection<string>> GatherAllPatternFiles(
		BatchConfiguration batch,
		string directory,
		Action<string>? progressCallback,
		IFileSystem fileSystem)
	{
		// Use parallel processing to find files for all patterns simultaneously
		ParallelQuery<(string pattern, IReadOnlyCollection<string> files)> patternResults =
			batch.FilePatterns.AsParallel().Select(pattern =>
			{
				IReadOnlyCollection<string> files = FileFinder.FindFiles(
					batch.SearchPaths,
					directory,
					pattern,
					batch.PathExclusionPatterns,
					fileSystem,
					progressCallback);
				return (pattern, files);
			});

		return patternResults.ToDictionary(result => result.pattern, result => result.files);
	}

	/// <summary>
	/// Hashes files using a work queue approach for optimal parallelization
	/// </summary>
	/// <param name="workItems">List of files to hash with their filenames</param>
	/// <param name="maxDegreeOfParallelism">Maximum degree of parallelism (0 for auto)</param>
	/// <param name="progressCallback">Optional callback for progress updates</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The computed hashes, along with the files that could not be read.</returns>
	private static HashingPhaseResult HashFilesInParallel(
		List<(string filePath, string fileName)> workItems,
		int maxDegreeOfParallelism = 0,
		Action<string>? progressCallback = null,
		IFileSystem? fileSystem = null)
	{
		Dictionary<string, string> results = [];
		List<FileHashFailure> failures = [];
		int completedItems = 0;
		int totalItems = workItems.Count;

		int actualParallelism = maxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : maxDegreeOfParallelism;
		progressCallback?.Invoke($"🔗 Starting parallel hashing of {totalItems} files with {actualParallelism} workers...");

		ParallelOptions parallelOptions = new()
		{
			MaxDegreeOfParallelism = actualParallelism
		};

		object lockObject = new();

		Parallel.ForEach(workItems, parallelOptions, workItem =>
		{
			try
			{
				string hash = FileHasher.ComputeFileHash(workItem.filePath, fileSystem);

				lock (lockObject)
				{
					results[workItem.filePath] = hash;
					completedItems++;

					// Report progress more frequently to ensure visibility: every 5% or every 5 files, whichever is smaller
					int progressInterval = Math.Max(1, Math.Min(5, totalItems / 20));
					if (completedItems % progressInterval == 0 || completedItems == totalItems)
					{
						progressCallback?.Invoke($"🔗 Hashed {completedItems}/{totalItems} files ({completedItems * 100.0 / totalItems:F1}%)");
					}
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Record the failure and continue processing other files. The file is deliberately
				// left out of the results so it is not grouped or merged, but it must not disappear
				// silently: callers surface it as a failed pattern.
				lock (lockObject)
				{
					failures.Add(new FileHashFailure(workItem.filePath, ex.Message));
					progressCallback?.Invoke($"❌ Error hashing {workItem.filePath}: {ex.Message}");
				}
			}
		});

		progressCallback?.Invoke($"✅ Completed hashing {results.Count}/{totalItems} files");
		return new HashingPhaseResult(results, failures.AsReadOnly());
	}

	/// <summary>
	/// Finds the pattern that would match the given filename
	/// </summary>
	/// <param name="fileName">The filename to match</param>
	/// <param name="patterns">The list of patterns to check</param>
	/// <returns>The first matching pattern, or the filename if no pattern matches</returns>
	private static string FindMatchingPattern(string fileName, IReadOnlyCollection<string> patterns)
	{
		foreach (string pattern in patterns)
		{
			// Simple pattern matching - could be enhanced with proper glob matching
			if (pattern.Contains('*') || pattern.Contains('?'))
			{
				// For now, return the pattern if it could match
				// This is a simplified approach - full glob matching would be more accurate
				if (IsPatternMatch(fileName, pattern))
				{
					return pattern;
				}
			}
			else if (string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase))
			{
				return pattern;
			}
		}

		// If no pattern matches, return the filename itself
		return fileName;
	}

	/// <summary>
	/// Simple pattern matching helper
	/// </summary>
	/// <param name="fileName">The filename to check</param>
	/// <param name="pattern">The pattern to match against</param>
	/// <returns>True if the filename matches the pattern</returns>
	private static bool IsPatternMatch(string fileName, string pattern)
	{
		// Simple wildcard matching - this could be enhanced
		if (pattern == "*")
		{
			return true;
		}

		if (pattern.StartsWith("*."))
		{
			string extension = pattern[2..];
			return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
		}

		return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Core method that processes a single file pattern with all options
	/// </summary>
	/// <remarks>
	/// A pattern as broad as <c>*.dat</c> can match binary files, which the merge pipeline would read
	/// as UTF-8 and write back, destroying every byte it could not decode. Those files are separated
	/// out here and reported as skipped, so the merge only ever sees text.
	/// </remarks>
	private static PatternResult ProcessSinglePatternCore(
		PatternProcessingParameters parameters,
		ProcessingCallbacks callbacks)
	{
		// Find all files matching the pattern
		IReadOnlyCollection<string> files = FileFinder.FindFiles(
			parameters.SearchPaths,
			parameters.Directory,
			parameters.Pattern,
			parameters.PathExclusionPatterns,
			parameters.FileSystem);

		IFileSystem patternFileSystem = parameters.FileSystem ?? FileSystemProvider.Current;
		List<string> binaryFiles = [.. files.Where(file => BinaryContentDetector.IsBinaryFile(file, patternFileSystem))];

		if (binaryFiles.Count == files.Count && binaryFiles.Count > 0)
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = true,
				Message = AllFilesBinaryMessage,
				FilesFound = 0,
				SkippedBinaryFiles = [.. binaryFiles]
			};
		}

		PatternResult result = ProcessTextFiles(
			parameters,
			callbacks,
			binaryFiles.Count == 0 ? files : [.. files.Except(binaryFiles)]);

		foreach (string binaryFile in binaryFiles)
		{
			result.SkippedBinaryFiles.Add(binaryFile);
		}

		return result;
	}

	/// <summary>
	/// Determines whether every file holds exactly the same content.
	/// </summary>
	/// <param name="files">The files to compare.</param>
	/// <param name="fileSystem">The file system to read through, or <see langword="null"/> for the real one.</param>
	/// <returns><see langword="true"/> when no two files differ, so there is nothing to merge.</returns>
	private static bool AreAllFilesIdentical(IEnumerable<string> files, IFileSystem? fileSystem)
	{
		string? firstContent = null;

		foreach (string file in files)
		{
			string content = fileSystem?.File.ReadAllText(file) ?? File.ReadAllText(file);

			if (firstContent == null)
			{
				firstContent = content;
			}
			else if (content != firstContent)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Processes the text files found for a pattern, merging their versions together.
	/// </summary>
	/// <param name="parameters">The parameters of the pattern being processed.</param>
	/// <param name="callbacks">The callbacks driving the merge.</param>
	/// <param name="files">The files to process, which are known not to be binary.</param>
	/// <returns>The result of processing the pattern.</returns>
	private static PatternResult ProcessTextFiles(
		PatternProcessingParameters parameters,
		ProcessingCallbacks callbacks,
		IReadOnlyCollection<string> files)
	{
		if (files.Count == 0)
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = false,
				Message = NoFilesFoundMessage,
				FilesFound = 0
			};
		}

		if (files.Count == 1)
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = true,
				Message = OnlyOneFileFoundMessage,
				FilesFound = 1
			};
		}

		if (AreAllFilesIdentical(files, parameters.FileSystem))
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = true,
				Message = AllFilesIdenticalMessage,
				FilesFound = files.Count
			};
		}

		// Files are different, need to merge
		MergeResult? mergeResult = null;

		// Sort files to ensure consistent order
		List<string> sortedFiles = [.. files.OrderBy(f => f)];

		IFileSystem fileSystem = parameters.FileSystem ?? FileSystemProvider.Current;

		// Calculate similarity between the first two files
		double similarity = FileDiffer.CalculateFileSimilarity(sortedFiles[0], sortedFiles[1], parameters.FileSystem);

		// Merge first two files
		mergeResult = callbacks.MergeCallback(sortedFiles[0], sortedFiles[1], null);

		if (mergeResult == null)
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = false,
				Message = "Merge operation was cancelled",
				FilesFound = files.Count
			};
		}

		// Consolidate the merged content onto the files it came from, so the next iteration merges
		// against the accumulated result rather than the original content of the previous file
		PatternResult? writeFailure = ApplyMergedContent(mergeResult, sortedFiles.Take(2), fileSystem, parameters, files.Count);

		if (writeFailure != null)
		{
			return writeFailure;
		}

		// Update status - use appropriate constructor parameters for MergeSessionStatus
		callbacks.StatusCallback(new MergeSessionStatus(
			CurrentIteration: 1,
			RemainingFilesCount: files.Count,
			CompletedMergesCount: 0,
			MostSimilarPair: new FileSimilarity(sortedFiles[0], sortedFiles[1], similarity)
		));

		// Continue merging with remaining files
		for (int i = 2; i < sortedFiles.Count; i++)
		{
			if (!callbacks.ContinuationCallback())
			{
				return new PatternResult
				{
					Pattern = parameters.Pattern,
					Success = false,
					Message = "Merge operation was cancelled by user",
					FilesFound = files.Count
				};
			}

			// Calculate similarity between this file and the accumulated merge held by the previous file
			double currentSimilarity = FileDiffer.CalculateFileSimilarity(sortedFiles[i - 1], sortedFiles[i], parameters.FileSystem);

			mergeResult = callbacks.MergeCallback(sortedFiles[i - 1], sortedFiles[i], null);

			if (mergeResult == null)
			{
				return new PatternResult
				{
					Pattern = parameters.Pattern,
					Success = false,
					Message = "Merge operation was cancelled",
					FilesFound = files.Count
				};
			}

			// Fold this merge into every file consolidated so far, so the next iteration builds on it
			writeFailure = ApplyMergedContent(mergeResult, sortedFiles.Take(i + 1), fileSystem, parameters, files.Count);

			if (writeFailure != null)
			{
				return writeFailure;
			}

			// Update status with appropriate constructor parameters
			callbacks.StatusCallback(new MergeSessionStatus(
				CurrentIteration: i,
				RemainingFilesCount: files.Count - i,
				CompletedMergesCount: i - 1,
				MostSimilarPair: new FileSimilarity(sortedFiles[i - 1], sortedFiles[i], currentSimilarity)
			));
		}

		return new PatternResult
		{
			Pattern = parameters.Pattern,
			Success = true,
			Message = MergeCompletedSuccessfullyMessage,
			FilesFound = files.Count
		};
	}

	/// <summary>
	/// Writes an accumulated merge result over every file that has been folded into it.
	/// </summary>
	/// <remarks>
	/// This is what lets a sequence of merges chain: the next merge reads its first side back from
	/// disk, so it sees everything merged so far rather than that file's original content. It mirrors
	/// how <see cref="IterativeMergeOrchestrator.StartIterativeMergeProcess"/> consolidates a group.
	/// </remarks>
	/// <param name="mergeResult">The merge result to write.</param>
	/// <param name="mergedFiles">The files that have been folded into <paramref name="mergeResult"/>.</param>
	/// <param name="fileSystem">The file system to write through.</param>
	/// <param name="parameters">The parameters of the pattern being processed, used to describe a failure.</param>
	/// <param name="filesFound">The number of files found for the pattern, used to describe a failure.</param>
	/// <returns>A failed <see cref="PatternResult"/> if the content could not be written, otherwise <see langword="null"/>.</returns>
	private static PatternResult? ApplyMergedContent(
		MergeResult mergeResult,
		IEnumerable<string> mergedFiles,
		IFileSystem fileSystem,
		PatternProcessingParameters parameters,
		int filesFound)
	{
		// Use the style the merge detected from its sources; resolving it here instead is what let
		// the same merge produce CRLF from one command and LF from another.
		string mergedContent = mergeResult.ToContent();

		try
		{
			foreach (string filePath in mergedFiles)
			{
				fileSystem.File.WriteAllText(filePath, mergedContent);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return new PatternResult
			{
				Pattern = parameters.Pattern,
				Success = false,
				Message = $"Failed to write merged content: {ex.Message}",
				FilesFound = filesFound
			};
		}

		return null;
	}

	/// <summary>
	/// Processes a single file pattern with search paths and exclusion patterns
	/// </summary>
	/// <param name="pattern">The file pattern to process</param>
	/// <param name="searchPaths">The search paths to use. If empty, uses the directory parameter.</param>
	/// <param name="directory">The default directory to search in</param>
	/// <param name="pathExclusionPatterns">Path exclusion patterns to apply</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The pattern processing result</returns>
	public static PatternResult ProcessSinglePatternWithPaths(
		string pattern,
		IReadOnlyCollection<string> searchPaths,
		string directory,
		IReadOnlyCollection<string> pathExclusionPatterns,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		IFileSystem? fileSystem = null) =>
		ProcessSinglePatternCore(
			new PatternProcessingParameters(pattern, searchPaths, directory, pathExclusionPatterns, fileSystem),
			new ProcessingCallbacks(mergeCallback, statusCallback, continuationCallback));

	/// <summary>
	/// Processes a single file pattern with search paths, exclusion patterns, and progress reporting
	/// </summary>
	/// <param name="pattern">The file pattern to process</param>
	/// <param name="searchPaths">The search paths to use. If empty, uses the directory parameter.</param>
	/// <param name="directory">The default directory to search in</param>
	/// <param name="pathExclusionPatterns">Path exclusion patterns to apply</param>
	/// <param name="callbacks">Processing callbacks for merge operations and progress reporting</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The pattern processing result</returns>
	public static PatternResult ProcessSinglePatternWithPaths(
		string pattern,
		IReadOnlyCollection<string> searchPaths,
		string directory,
		IReadOnlyCollection<string> pathExclusionPatterns,
		ProcessingCallbacks callbacks,
		IFileSystem? fileSystem = null)
	{
		Ensure.NotNull(callbacks);

		return ProcessSinglePatternCore(
			new PatternProcessingParameters(pattern, searchPaths, directory, pathExclusionPatterns, fileSystem),
			callbacks);
	}

	/// <summary>
	/// Processes a single file pattern
	/// </summary>
	/// <param name="pattern">The file pattern to process</param>
	/// <param name="directory">The directory to search in</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The pattern processing result</returns>
	public static PatternResult ProcessSinglePattern(
		string pattern,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		IFileSystem? fileSystem = null)
	{
		return ProcessSinglePatternWithPaths(
			pattern,
			[],
			directory,
			[],
			mergeCallback,
			statusCallback,
			continuationCallback,
			fileSystem);
	}

	/// <summary>
	/// Processes a single file pattern with progress reporting
	/// </summary>
	/// <param name="pattern">The file pattern to process</param>
	/// <param name="directory">The directory to search in</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <param name="progressCallback">Optional callback to report discovered file paths</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to real filesystem)</param>
	/// <returns>The pattern processing result</returns>
	public static PatternResult ProcessSinglePattern(
		string pattern,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		Action<string>? progressCallback,
		IFileSystem? fileSystem = null) =>
		ProcessSinglePatternCore(
			new PatternProcessingParameters(pattern, [], directory, [], fileSystem),
			new ProcessingCallbacks(mergeCallback, statusCallback, continuationCallback, progressCallback));

	/// <summary>
	/// Creates a custom batch configuration from a list of patterns
	/// </summary>
	/// <param name="name">Name for the batch</param>
	/// <param name="patterns">List of file patterns</param>
	/// <param name="description">Optional description</param>
	/// <returns>A new batch configuration</returns>
	public static BatchConfiguration CreateCustomBatch(string name, IEnumerable<string> patterns, string description = "")
	{
		Ensure.NotNull(name);
		Ensure.NotNull(patterns);

		return new BatchConfiguration
		{
			Name = name,
			Description = description,
			FilePatterns = [.. patterns.Where(p => !string.IsNullOrWhiteSpace(p))],
			SkipEmptyPatterns = true,
			PromptBeforeEachPattern = false
		};
	}
}
