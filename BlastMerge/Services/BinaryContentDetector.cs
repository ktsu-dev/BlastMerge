// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Unicode;

/// <summary>
/// Decides whether file content is binary, so the text diff and merge pipeline never decodes bytes
/// it cannot write back.
/// </summary>
/// <remarks>
/// The pipeline reads files with <c>ReadAllText</c>/<c>ReadAllLines</c> and writes the merged result
/// back with <c>WriteAllText</c>. That round trip only preserves content that is valid UTF-8:
/// .NET's decoder silently replaces every invalid byte sequence with U+FFFD rather than throwing, so
/// by the time anything is written the original bytes are already gone. A glob that matches an icon
/// or a serialized asset would otherwise walk straight into that path and overwrite the file with a
/// lossy transcription of itself, with ASCII conflict markers inserted into the byte stream and no
/// warning to the user. Detecting binary content up front is what keeps the tool to "same or
/// different" on those files, the way <c>git diff</c> treats them.
/// </remarks>
public static class BinaryContentDetector
{
	/// <summary>
	/// The number of leading bytes inspected when deciding whether content is binary.
	/// </summary>
	/// <remarks>
	/// Matches the sample size git uses. A file that is text for its first 8000 bytes and binary
	/// after them is pathological; reading whole files to rule it out would cost every comparison.
	/// </remarks>
	public const int SampleSize = 8000;

	private const byte ContinuationByteMask = 0xC0;
	private const byte ContinuationBytePattern = 0x80;
	private const byte FirstMultiByteLeadByte = 0xC0;
	private const int MaxContinuationBytes = 3;

	/// <summary>
	/// Determines whether the given content is binary.
	/// </summary>
	/// <param name="content">The raw bytes to inspect, in full.</param>
	/// <returns><see langword="true"/> if the content cannot be treated as UTF-8 text.</returns>
	/// <remarks>
	/// Content counts as binary when it contains a NUL byte, which is git's rule, or when it is not
	/// valid UTF-8, which is the condition that makes the read/write round trip lossy. The second
	/// test also catches text in another encoding, such as UTF-16 or a single-byte code page: that
	/// content is not binary in the strict sense, but this pipeline would corrupt it just the same.
	/// </remarks>
	public static bool IsBinary(ReadOnlySpan<byte> content) => IsBinary(content, truncated: false);

	/// <summary>
	/// Determines whether a file holds binary content, inspecting at most <see cref="SampleSize"/> bytes.
	/// </summary>
	/// <param name="filePath">Path to the file to inspect.</param>
	/// <param name="fileSystem">File system abstraction (optional, defaults to <see cref="FileSystemProvider.Current"/>).</param>
	/// <returns>
	/// <see langword="true"/> if the file's leading bytes are binary as described on
	/// <see cref="IsBinary(ReadOnlySpan{byte})"/>. A file that does not exist is not binary, leaving
	/// the caller's own missing-file handling in charge.
	/// </returns>
	public static bool IsBinaryFile(string filePath, IFileSystem? fileSystem = null)
	{
		Ensure.NotNull(filePath);

		fileSystem ??= FileSystemProvider.Current;

		if (!fileSystem.File.Exists(filePath))
		{
			return false;
		}

		byte[] buffer = new byte[SampleSize];
		int read;

		using (Stream stream = fileSystem.File.OpenRead(filePath))
		{
			read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
		}

		// Only a full sample can have been cut mid-character; a short read reached the end of the file.
		return IsBinary(buffer.AsSpan(0, read), truncated: read == buffer.Length);
	}

	/// <summary>
	/// Determines whether a sample of content is binary.
	/// </summary>
	/// <param name="content">The sampled bytes.</param>
	/// <param name="truncated">Whether <paramref name="content"/> stops short of the end of the file.</param>
	/// <returns><see langword="true"/> if the content cannot be treated as UTF-8 text.</returns>
	private static bool IsBinary(ReadOnlySpan<byte> content, bool truncated)
	{
		if (content.IsEmpty)
		{
			return false;
		}

		if (content.IndexOf((byte)0) >= 0)
		{
			return true;
		}

		return !Utf8.IsValid(truncated ? TrimIncompleteSequence(content) : content);
	}

	/// <summary>
	/// Drops a trailing UTF-8 sequence that the sample boundary cut in half.
	/// </summary>
	/// <param name="content">The sampled bytes.</param>
	/// <returns>The sample without its final, possibly incomplete, character.</returns>
	/// <remarks>
	/// A sample cut at a fixed length can end in the middle of a multi-byte character, which would
	/// otherwise read as invalid UTF-8 and condemn a perfectly ordinary text file. Trimming the last
	/// character costs nothing: at most four bytes of an 8000-byte sample go uninspected.
	/// </remarks>
	private static ReadOnlySpan<byte> TrimIncompleteSequence(ReadOnlySpan<byte> content)
	{
		int end = content.Length;
		int continuationBytes = 0;

		while (end > 0 && continuationBytes < MaxContinuationBytes && IsContinuationByte(content[end - 1]))
		{
			end--;
			continuationBytes++;
		}

		if (end > 0 && content[end - 1] >= FirstMultiByteLeadByte)
		{
			end--;
		}

		return content[..end];
	}

	/// <summary>
	/// Determines whether a byte continues a multi-byte UTF-8 sequence.
	/// </summary>
	/// <param name="value">The byte to classify.</param>
	/// <returns><see langword="true"/> if the byte is a continuation byte.</returns>
	private static bool IsContinuationByte(byte value) => (value & ContinuationByteMask) == ContinuationBytePattern;
}
