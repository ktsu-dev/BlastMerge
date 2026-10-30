// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

/// <summary>
/// Writes a merge result over every copy of a file, so that either all of them take it or none do.
/// </summary>
internal static class MergedContentWriter
{
	/// <summary>
	/// Writes <paramref name="content"/> to every file in <paramref name="filePaths"/>. If any write fails,
	/// the files already written are put back to their original bytes before the failure is rethrown.
	/// </summary>
	/// <remarks>
	/// Without this, one unwritable copy (a read-only Perforce file, a file an editor has locked) left
	/// the copies before it merged and the copies after it unchanged: more versions than before the merge.
	/// </remarks>
	/// <param name="fileSystem">The file system to write through.</param>
	/// <param name="filePaths">The files to write.</param>
	/// <param name="content">The merged content.</param>
	/// <param name="encoding">The encoding to write the content in.</param>
	/// <exception cref="IOException">
	/// A write failed. When a file could not be put back, the message names it, since it now holds the merged content.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">A write was refused, and every file was put back.</exception>
	public static void WriteToAll(IFileSystem fileSystem, IEnumerable<string> filePaths, string content, Encoding encoding)
	{
		Ensure.NotNull(fileSystem);
		Ensure.NotNull(filePaths);
		Ensure.NotNull(content);
		Ensure.NotNull(encoding);

		List<(string Path, byte[] Original)> originals = [.. filePaths.Distinct().Select(path => (path, fileSystem.File.ReadAllBytes(path)))];

		int attempted = 0;
		try
		{
			foreach ((string path, _) in originals)
			{
				attempted++;
				fileSystem.File.WriteAllText(path, content, encoding);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// The failing write is included: a write that fails part-way can leave the file truncated.
			List<string> unrestored = [.. originals.Take(attempted).Where(file => !TryRestore(fileSystem, file.Path, file.Original)).Select(file => file.Path)];

			if (unrestored.Count > 0)
			{
				throw new IOException($"{ex.Message} Could not restore {string.Join(", ", unrestored)}, which may now hold the merged content.", ex);
			}

			throw;
		}
	}

	/// <summary>
	/// Puts a file back to its original bytes, leaving it alone if it still holds them.
	/// </summary>
	/// <param name="fileSystem">The file system to write through.</param>
	/// <param name="path">The file to restore.</param>
	/// <param name="original">The file's bytes before the merge was written.</param>
	/// <returns><see langword="true"/> if the file holds its original bytes afterwards.</returns>
	private static bool TryRestore(IFileSystem fileSystem, string path, byte[] original)
	{
		try
		{
			if (!fileSystem.File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
			{
				fileSystem.File.WriteAllBytes(path, original);
			}

			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}
}
