// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

/// <summary>
/// Represents a request for batch processing in a directory.
/// </summary>
/// <param name="Directory">The directory to process.</param>
/// <param name="BatchName">The name of the batch configuration.</param>
public record BatchRequest(string Directory, string BatchName)
{
	/// <summary>
	/// Validates the batch request.
	/// </summary>
	/// <returns>True if the request is valid, false otherwise.</returns>
	public bool IsValid() =>
		!string.IsNullOrWhiteSpace(Directory) &&
		!string.IsNullOrWhiteSpace(BatchName);
}
