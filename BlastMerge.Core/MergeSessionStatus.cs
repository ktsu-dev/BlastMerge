// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Represents the status of an iterative merge process
/// </summary>
/// <param name="CurrentIteration"> Gets the current iteration number </param>
/// <param name="RemainingFilesCount"> Gets the number of remaining files to merge </param>
/// <param name="CompletedMergesCount"> Gets the number of completed merges </param>
/// <param name="MostSimilarPair"> Gets the most similar file pair for this iteration </param>
public record MergeSessionStatus(int CurrentIteration, int RemainingFilesCount, int CompletedMergesCount, FileSimilarity? MostSimilarPair);
