// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

/// <summary>
/// Interface for accessing environment-specific information
/// </summary>
public interface IEnvironmentProvider
{
	/// <summary>
	/// Gets the number of processors on the current machine
	/// </summary>
	public int ProcessorCount { get; }

	/// <summary>
	/// Gets the newline string defined for this environment
	/// </summary>
	public string NewLine { get; }
}
