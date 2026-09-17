// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;

/// <summary>
/// Thrown when a text merge is asked to combine content that <see cref="BinaryContentDetector"/>
/// classifies as binary.
/// </summary>
/// <remarks>
/// Merging binary content as text destroys it, so the merge paths refuse rather than produce a
/// result. Callers that choose which versions to merge — <see cref="FileDiffer.FindMostSimilarFiles"/>
/// and the batch pipeline — leave binary files out of that choice, so reaching this exception means a
/// caller bypassed the selection rather than that a user asked for something unsupported.
/// </remarks>
public class BinaryContentException : InvalidOperationException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BinaryContentException"/> class.
	/// </summary>
	public BinaryContentException()
		: base("Binary content cannot be merged as text.")
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BinaryContentException"/> class with a message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public BinaryContentException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BinaryContentException"/> class with a message
	/// and an inner exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that caused this one.</param>
	public BinaryContentException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Creates an exception naming the two files that could not be merged.
	/// </summary>
	/// <param name="file1">Path to the first file.</param>
	/// <param name="file2">Path to the second file.</param>
	/// <returns>An exception describing the refusal.</returns>
	public static BinaryContentException ForFiles(string file1, string file2) =>
		new($"Refusing to merge binary content as text: '{file1}' and '{file2}'. Merging them would decode their bytes as UTF-8 and write the result back, losing every byte that is not valid UTF-8.");
}
