// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Linq;
using System.Text;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BinaryContentDetector"/>.
/// </summary>
/// <remarks>
/// The detector decides whether the diff and merge pipeline is allowed to read a file as text, so a
/// false negative costs the file: the pipeline decodes it as UTF-8, replaces every byte it cannot
/// represent with U+FFFD, and writes the result back. A false positive only costs a merge that the
/// user has to do by hand, so the cases below lean on content that really cannot survive the round
/// trip rather than on guesses about file types.
/// </remarks>
[TestClass]
public class BinaryContentDetectorTests : MockFileSystemTestBase
{
	[TestMethod]
	public void IsBinary_WithEmptyContent_ReturnsFalse()
	{
		// Act & Assert - an empty file merges as text perfectly well
		Assert.IsFalse(BinaryContentDetector.IsBinary([]), "Empty content should not be treated as binary");
	}

	[TestMethod]
	public void IsBinary_WithPlainAsciiText_ReturnsFalse()
	{
		// Arrange
		byte[] content = Encoding.UTF8.GetBytes("line one\nline two\n");

		// Act & Assert
		Assert.IsFalse(BinaryContentDetector.IsBinary(content), "ASCII text should not be treated as binary");
	}

	[TestMethod]
	public void IsBinary_WithMultiByteUtf8Text_ReturnsFalse()
	{
		// Arrange - accents, a CJK character and an emoji cover the two, three and four byte forms
		byte[] content = Encoding.UTF8.GetBytes("café\n日本語\n🚀\n");

		// Act & Assert
		Assert.IsFalse(BinaryContentDetector.IsBinary(content), "Valid multi-byte UTF-8 should not be treated as binary");
	}

	[TestMethod]
	public void IsBinary_WithNulByte_ReturnsTrue()
	{
		// Arrange - a NUL byte is git's own signal that content is not text
		byte[] content = [0x54, 0x65, 0x78, 0x74, 0x00, 0x54, 0x65, 0x78, 0x74];

		// Act & Assert
		Assert.IsTrue(BinaryContentDetector.IsBinary(content), "Content containing a NUL byte should be treated as binary");
	}

	[TestMethod]
	public void IsBinary_WithInvalidUtf8Sequence_ReturnsTrue()
	{
		// Arrange - 0xFF never appears in valid UTF-8, so decoding this content loses those bytes
		byte[] content = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0xFF, 0xFE, 0x0A];

		// Act & Assert
		Assert.IsTrue(BinaryContentDetector.IsBinary(content), "Content that is not valid UTF-8 should be treated as binary");
	}

	[TestMethod]
	public void IsBinary_WithLoneContinuationByte_ReturnsTrue()
	{
		// Arrange - a continuation byte with no lead byte is invalid, and decodes to U+FFFD
		byte[] content = [0x61, 0x80, 0x62];

		// Act & Assert
		Assert.IsTrue(BinaryContentDetector.IsBinary(content), "A dangling UTF-8 continuation byte should be treated as binary");
	}

	[TestMethod]
	public void IsBinaryFile_WithTextLongerThanTheSample_ReturnsFalse()
	{
		// Arrange - fill well past the sample size with a multi-byte character, so the sample almost
		// certainly ends mid-sequence. Truncation alone must not condemn an ordinary text file.
		string text = string.Concat(Enumerable.Repeat("é", BinaryContentDetector.SampleSize));
		string filePath = CreateBinaryFile("long-text.txt", Encoding.UTF8.GetBytes(text));

		// Act & Assert
		Assert.IsFalse(BinaryContentDetector.IsBinaryFile(filePath, MockFileSystem), "Text longer than the sample should not be treated as binary");
	}

	[TestMethod]
	public void IsBinaryFile_WithBinaryContent_ReturnsTrue()
	{
		// Arrange - the leading bytes of a PNG, which carry both a NUL and invalid UTF-8
		byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
		string filePath = CreateBinaryFile("image.png", content);

		// Act & Assert
		Assert.IsTrue(BinaryContentDetector.IsBinaryFile(filePath, MockFileSystem), "Binary file content should be detected");
	}

	[TestMethod]
	public void IsBinaryFile_WithMissingFile_ReturnsFalse()
	{
		// Arrange - create the file inside this test's tree, then take it away again
		string filePath = CreateBinaryFile("not-here.txt", [0x74, 0x65, 0x78, 0x74]);
		MockFileSystem.File.Delete(filePath);

		// Act & Assert - the caller's own missing-file handling stays in charge
		Assert.IsFalse(BinaryContentDetector.IsBinaryFile(filePath, MockFileSystem), "A file that does not exist should not be reported as binary");
	}

	[TestMethod]
	public void IsBinaryFile_WithNullPath_ThrowsArgumentNullException() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => BinaryContentDetector.IsBinaryFile(null!, MockFileSystem));
}
