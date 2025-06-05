// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System.Text;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Service for handling interactive merge UI operations.
/// </summary>
public static class InteractiveMergeService
{
	/// <summary>
	/// Performs iterative merge on file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to merge.</param>
	public static void PerformIterativeMerge(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);

		// Convert to file groups by hash to find groups with multiple versions
		List<string> allFiles = [.. fileGroups.SelectMany(g => g.Value)];
		IReadOnlyCollection<FileGroup> groups = FileDiffer.GroupFilesByHash(allFiles);

		// Filter to groups with multiple files for merging
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No groups with multiple files to merge.[/]");
			return;
		}

		AnsiConsole.MarkupLine($"[cyan]Found {groupsWithMultipleFiles.Count} groups with multiple versions that can be merged.[/]");
		AnsiConsole.WriteLine();

		foreach (FileGroup group in groupsWithMultipleFiles)
		{
			PerformGroupMerge(group);
		}
	}

	/// <summary>
	/// Performs iterative merge on a single file group.
	/// </summary>
	/// <param name="group">The file group to merge.</param>
	private static void PerformGroupMerge(FileGroup group)
	{
		List<string> remainingFiles = [.. group.FilePaths];
		AnsiConsole.MarkupLine($"[cyan]Starting iterative merge for {remainingFiles.Count} files...[/]");

		while (remainingFiles.Count > 1)
		{
			if (!ProcessSingleMergeStep(remainingFiles))
			{
				break;
			}
		}

		ShowFinalResult(remainingFiles);
	}

	/// <summary>
	/// Processes a single merge step between the most similar files.
	/// </summary>
	/// <param name="remainingFiles">List of remaining files to merge.</param>
	/// <returns>True if merge step was successful, false if should stop.</returns>
	private static bool ProcessSingleMergeStep(List<string> remainingFiles)
	{
		FileSimilarity? mostSimilar = FindMostSimilarInGroup(remainingFiles);
		if (mostSimilar == null)
		{
			AnsiConsole.MarkupLine("[red]Could not find similar files to merge.[/]");
			return false;
		}

		ShowMergeInfo(mostSimilar);
		MergeResult mergeResult = FileDiffer.MergeFiles(mostSimilar.FilePath1, mostSimilar.FilePath2);

		return HandleMergeConflicts(mergeResult) && ApplyMergeResult(mergeResult, mostSimilar, remainingFiles);
	}

	/// <summary>
	/// Shows information about the files being merged.
	/// </summary>
	/// <param name="similarity">The file similarity information.</param>
	private static void ShowMergeInfo(FileSimilarity similarity)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[yellow]Merging most similar files ({similarity.SimilarityScore:P1} similar):[/]");
		AnsiConsole.MarkupLine($"  [green]{similarity.FilePath1}[/]");
		AnsiConsole.MarkupLine($"  [green]{similarity.FilePath2}[/]");
	}

	/// <summary>
	/// Handles merge conflicts by showing preview and getting user confirmation.
	/// </summary>
	/// <param name="mergeResult">The merge result to handle.</param>
	/// <returns>True if user accepts merge, false otherwise.</returns>
	private static bool HandleMergeConflicts(MergeResult mergeResult)
	{
		if (mergeResult.Conflicts.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Clean merge - no conflicts detected![/]");
			return true;
		}

		AnsiConsole.MarkupLine("[yellow]Merge has conflicts. Displaying merge with conflict markers...[/]");
		ShowMergePreview(mergeResult);

		if (!AnsiConsole.Confirm("[yellow]Accept this merge (with conflict markers)?[/]"))
		{
			AnsiConsole.MarkupLine("[yellow]Merge cancelled. Skipping this group.[/]");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Applies the merge result by saving files and updating the remaining files list.
	/// </summary>
	/// <param name="mergeResult">The merge result to apply.</param>
	/// <param name="similarity">The file similarity information.</param>
	/// <param name="remainingFiles">List of remaining files to update.</param>
	/// <returns>True if application was successful, false otherwise.</returns>
	private static bool ApplyMergeResult(MergeResult mergeResult, FileSimilarity similarity, List<string> remainingFiles)
	{
		try
		{
			string mergedContent = string.Join('\n', mergeResult.MergedLines);
			File.WriteAllText(similarity.FilePath1, mergedContent);

			UpdateMatchingFiles(remainingFiles, similarity, mergedContent);
			remainingFiles.Remove(similarity.FilePath2);

			AnsiConsole.MarkupLine($"[green]Merged successfully! Versions reduced by 1. ({remainingFiles.Count} remaining)[/]");
			if (remainingFiles.Count > 1)
			{
				AnsiConsole.MarkupLine("[dim]Continuing to next merge step...[/]");
			}

			return true;
		}
		catch (IOException ex)
		{
			AnsiConsole.MarkupLine($"[red]Failed to save merge result: {ex.Message}[/]");
			return false;
		}
		catch (UnauthorizedAccessException ex)
		{
			AnsiConsole.MarkupLine($"[red]Access denied when saving merge result: {ex.Message}[/]");
			return false;
		}
	}

	/// <summary>
	/// Updates all files that have the same content as the second file.
	/// </summary>
	/// <param name="remainingFiles">List of remaining files.</param>
	/// <param name="similarity">The file similarity information.</param>
	/// <param name="mergedContent">The merged content to write.</param>
	private static void UpdateMatchingFiles(List<string> remainingFiles, FileSimilarity similarity, string mergedContent)
	{
		string file2Content = File.ReadAllText(similarity.FilePath2);
		foreach (string file in remainingFiles)
		{
			if (file != similarity.FilePath1 && File.ReadAllText(file) == file2Content)
			{
				File.WriteAllText(file, mergedContent);
			}
		}
	}

	/// <summary>
	/// Shows the final result of the merge process.
	/// </summary>
	/// <param name="remainingFiles">List of remaining files.</param>
	private static void ShowFinalResult(List<string> remainingFiles)
	{
		if (remainingFiles.Count == 1)
		{
			AnsiConsole.MarkupLine("[green]Final merged result is saved in the remaining file.[/]");
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
		Console.ReadKey();
	}

	/// <summary>
	/// Finds the most similar pair of files in a group.
	/// </summary>
	/// <param name="files">The files to compare.</param>
	/// <returns>The most similar file pair, or null if none found.</returns>
	private static FileSimilarity? FindMostSimilarInGroup(List<string> files)
	{
		if (files.Count < 2)
		{
			return null;
		}

		FileSimilarity? mostSimilar = null;
		double highestSimilarity = 0;

		for (int i = 0; i < files.Count; i++)
		{
			for (int j = i + 1; j < files.Count; j++)
			{
				try
				{
					double similarity = FileDiffer.CalculateFileSimilarity(files[i], files[j]);

					if (similarity > highestSimilarity)
					{
						highestSimilarity = similarity;
						mostSimilar = new FileSimilarity(files[i], files[j], similarity);
					}
				}
				catch (IOException)
				{
					// Skip files that can't be read
				}
			}
		}

		return mostSimilar;
	}

	/// <summary>
	/// Shows a preview of the merge result with conflicts highlighted.
	/// </summary>
	/// <param name="mergeResult">The merge result to preview.</param>
	private static void ShowMergePreview(MergeResult mergeResult)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[cyan]Merge Preview (first 50 lines):[/]");
		AnsiConsole.WriteLine();

		Panel panel = new(GetMergePreviewContent(mergeResult))
		{
			Border = BoxBorder.Rounded,
			BorderStyle = new Style(Color.Yellow),
			Header = new PanelHeader(" Merged Content ")
		};

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Gets the merge preview content with conflict highlighting.
	/// </summary>
	/// <param name="mergeResult">The merge result to get content from.</param>
	/// <returns>The formatted merge preview content.</returns>
	private static string GetMergePreviewContent(MergeResult mergeResult)
	{
		string[] lines = [.. mergeResult.MergedLines];
		StringBuilder preview = new();
		int lineCount = 0;
		const int maxLines = 50;

		foreach (string line in lines)
		{
			if (lineCount >= maxLines)
			{
				preview.AppendLine("[dim]... (content truncated)[/]");
				break;
			}

			string formattedLine;

			// Highlight conflict markers
			if (line.StartsWith("<<<<<<<"))
			{
				formattedLine = $"[red bold]{line}[/]";
			}
			else if (line.StartsWith("======="))
			{
				formattedLine = $"[yellow bold]{line}[/]";
			}
			else if (line.StartsWith(">>>>>>>"))
			{
				formattedLine = $"[blue bold]{line}[/]";
			}
			else
			{
				// Escape markup in regular content
				formattedLine = line.Replace("[", "[[").Replace("]", "]]");
			}

			preview.AppendLine(formattedLine);
			lineCount++;
		}

		return preview.ToString();
	}
}
