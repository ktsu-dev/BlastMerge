// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.IO;
using System.IO.Abstractions;

/// <summary>
/// Detects whether a file starts with the UTF-8 byte order mark.
/// </summary>
/// <remarks>
/// The merge pipeline reads files with <c>ReadAllText</c>, which drops the byte order mark, so the
/// merged text alone cannot say whether the sources carried one. Visual Studio saves C# files with a
/// byte order mark by default, and writing them back without it turns every merge into a whole-file
/// change and puts the merged copies in a different hash group from untouched ones.
/// </remarks>
public static class Utf8BomDetector
{
	private static readonly byte[] Utf8Preamble = [0xEF, 0xBB, 0xBF];

	/// <summary>
	/// Determines whether a file starts with the UTF-8 byte order mark.
	/// </summary>
	/// <param name="filePath">Path to the file to inspect.</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to <see cref="FileSystemProvider.Current"/>).</param>
	/// <returns>
	/// <see langword="true"/> if the file's first bytes are <c>EF BB BF</c>. A file that does not exist
	/// has no byte order mark.
	/// </returns>
	public static bool HasUtf8Bom(string filePath, IFileSystem? fileSystem = null)
	{
		Ensure.NotNull(filePath);

		fileSystem ??= FileSystemProvider.Current;

		if (!fileSystem.File.Exists(filePath))
		{
			return false;
		}

		byte[] buffer = new byte[Utf8Preamble.Length];
		int read;

		using (Stream stream = fileSystem.File.OpenRead(filePath))
		{
			read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
		}

		return read == Utf8Preamble.Length && buffer.AsSpan().SequenceEqual(Utf8Preamble);
	}

	/// <summary>
	/// Determines whether merged content from two files should be written with a byte order mark.
	/// </summary>
	/// <param name="file1">Path to the first file.</param>
	/// <param name="file2">Path to the second file.</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to <see cref="FileSystemProvider.Current"/>).</param>
	/// <returns><see langword="true"/> if either file starts with the UTF-8 byte order mark.</returns>
	/// <remarks>
	/// Either side carrying one is enough, the same rule <see cref="LineEndingDetector.EndsWithLineEnding(string, string)"/>
	/// applies to the trailing line ending.
	/// </remarks>
	public static bool HasUtf8Bom(string file1, string file2, IFileSystem? fileSystem = null) =>
		HasUtf8Bom(file1, fileSystem) || HasUtf8Bom(file2, fileSystem);
}
