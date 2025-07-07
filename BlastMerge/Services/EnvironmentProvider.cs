// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using ktsu.BlastMerge.Contracts;

/// <summary>
/// Default implementation of environment provider
/// </summary>
public class EnvironmentProvider : IEnvironmentProvider
{
	/// <summary>
	/// Gets the number of processors on the current machine
	/// </summary>
	public int ProcessorCount => Environment.ProcessorCount;

	/// <summary>
	/// Gets the newline string defined for this environment
	/// </summary>
	public string NewLine => Environment.NewLine;
}
