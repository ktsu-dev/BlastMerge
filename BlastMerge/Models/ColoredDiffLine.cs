// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a diffed line with formatting information
/// </summary>
/// <param name="Content"> Gets or sets the line content </param>
/// <param name="Color"> Gets or sets the color for this line </param>
public record ColoredDiffLine(string Content, DiffColor Color);

