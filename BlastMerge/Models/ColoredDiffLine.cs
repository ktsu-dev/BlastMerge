// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a diffed line with formatting information
/// </summary>
/// <param name="Content"> Gets or sets the line content </param>
/// <param name="Color"> Gets or sets the color for this line </param>
public record ColoredDiffLine(string Content, DiffColor Color);

