// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.IO.Abstractions;
using System.Threading;

/// <summary>
/// Provides centralized access to filesystem implementations
/// </summary>
public static class FileSystemProvider
{
	// Use the Lazy<T> pattern for thread-safe initialization of the default instance
	private static readonly Lazy<IFileSystem> _defaultInstance = new(() => new FileSystem());

	// Use AsyncLocal to store a filesystem instance per async execution context
	// This ensures different test threads get their own isolated instances
	private static readonly AsyncLocal<IFileSystem?> _asyncLocalInstance = new();

	/// <summary>
	/// Gets the current filesystem instance
	/// </summary>
	public static IFileSystem Current
	{
		get
		{
			// First check if we have a context-specific instance set
			if (_asyncLocalInstance.Value != null)
			{
				return _asyncLocalInstance.Value;
			}

			// Otherwise return the default lazy-initialized instance
			return _defaultInstance.Value;
		}
	}

	/// <summary>
	/// Sets the filesystem implementation to be used by the current async context
	/// </summary>
	/// <param name="fileSystem">The filesystem implementation</param>
	public static void SetFileSystem(IFileSystem fileSystem)
	{
		ArgumentNullException.ThrowIfNull(fileSystem);
		_asyncLocalInstance.Value = fileSystem;
	}

	/// <summary>
	/// Resets the filesystem back to the default implementation for the current async context
	/// </summary>
	public static void ResetToDefault() => _asyncLocalInstance.Value = null;
}
