// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System.IO.Abstractions;

/// <summary>
/// Provides file system operations with support for testing and dependency injection.
/// </summary>
public static class FileSystemProvider
{
	private static readonly ktsu.FileSystemProvider.FileSystemProvider _provider = new();

	/// <summary>
	/// Gets the current file system instance.
	/// </summary>
	public static IFileSystem Current => _provider.Current;

	/// <summary>
	/// Sets a custom file system factory for testing.
	/// </summary>
	/// <param name="factory">The file system factory to use.</param>
	public static void SetInstance(Func<IFileSystem> factory) => _provider.SetFileSystemFactory(factory);

	/// <summary>
	/// Sets a custom file system for testing.
	/// </summary>
	/// <param name="fileSystem">The file system to use.</param>
	public static void SetFileSystem(IFileSystem fileSystem) => _provider.SetFileSystemFactory(() => fileSystem);

	/// <summary>
	/// Resets the file system provider to the default implementation.
	/// </summary>
	public static void ResetToDefault() => _provider.ResetToDefault();
}
