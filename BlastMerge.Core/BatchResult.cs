// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;
using System.Collections.ObjectModel;

public static partial class BatchProcessor
{
	/// <summary>
	/// Represents the result of processing an entire batch
	/// </summary>
	public class BatchResult
	{
		/// <inheritdoc/>
		public string BatchName { get; set; } = string.Empty;
		/// <inheritdoc/>
		public bool Success { get; set; }
		/// <inheritdoc/>
		public Collection<PatternResult> PatternResults { get; init; } = [];
		/// <inheritdoc/>
		public int TotalPatternsProcessed { get; set; }
		/// <inheritdoc/>
		public int SuccessfulPatterns { get; set; }
		/// <inheritdoc/>
		public string Summary { get; set; } = string.Empty;
	}
}
