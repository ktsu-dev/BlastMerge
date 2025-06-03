// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Represents the completion status of an iterative merge
/// </summary>
/// <param name="IsSuccessful"> Gets whether the merge completed successfully </param>
/// <param name="FinalMergedContent"> Gets the final merged content </param>
/// <param name="FinalLineCount"> Gets the number of lines in the final result </param>
/// <param name="OriginalFileName"> Gets the original filename being merged </param>
public record MergeCompletionResult(bool IsSuccessful, string? FinalMergedContent, int FinalLineCount, string OriginalFileName);
