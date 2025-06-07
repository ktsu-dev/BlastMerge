// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents a line difference between two files
/// </summary>
/// <param name="LineNumber1"> Gets or sets the line number in the first file </param>
/// <param name="LineNumber2"> Gets or sets the line number in the second file </param>
/// <param name="Content1"> Gets or sets the content from the first file </param>
/// <param name="Content2"> Gets or sets the content from the second file </param>
/// <param name="Type"> Gets or sets the type of difference </param>
public record LineDifference(int? LineNumber1, int? LineNumber2, string? Content1, string? Content2, LineDifferenceType Type);

