// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a request for processing files in a directory.
/// </summary>
/// <param name="Directory">The directory containing files to process.</param>
/// <param name="FileName">The filename pattern to search for.</param>
public record ProcessingRequest(string Directory, string FileName)
{
	/// <summary>
	/// Validates the processing request.
	/// </summary>
	/// <returns>True if the request is valid, false otherwise.</returns>
	public bool IsValid() =>
		!string.IsNullOrWhiteSpace(Directory) &&
		!string.IsNullOrWhiteSpace(FileName);
}
