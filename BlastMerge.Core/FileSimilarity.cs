// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Represents the result of a file similarity calculation
/// </summary>
/// <param name="FilePath1"> Gets the path to the first file </param>
/// <param name="FilePath2"> Gets the path to the second file </param>
/// <param name="SimilarityScore"> Gets the similarity score between 0.0 (completely different) and 1.0 (identical) </param>
public record FileSimilarity(string FilePath1, string FilePath2, double SimilarityScore);

