// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using System.Text;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="MergedContentWriter"/>.
/// </summary>
[TestClass]
public class MergedContentWriterTests : MockFileSystemTestBase
{
	/// <summary>
	/// Every file takes the merged content when every file can be written.
	/// </summary>
	[TestMethod]
	public void WriteToAll_AllWritable_WritesEveryFile()
	{
		string first = CreateFile("first.txt", "old one\n");
		string second = CreateFile("second.txt", "old two\n");

		MergedContentWriter.WriteToAll(MockFileSystem, [first, second], "merged\n", Encoding.UTF8);

		Assert.AreEqual("merged\n", MockFileSystem.File.ReadAllText(first));
		Assert.AreEqual("merged\n", MockFileSystem.File.ReadAllText(second));
	}

	/// <summary>
	/// A file that can't be written stops the write, and the files written before it get their original
	/// bytes back, so no file is left holding the merged content.
	/// </summary>
	[TestMethod]
	public void WriteToAll_MiddleFileReadOnly_RestoresFilesAlreadyWrittenAndRethrows()
	{
		string first = CreateFile("first.txt", "old one\r\n");
		string locked = CreateFile("locked.txt", "old two\n");
		string last = CreateFile("last.txt", "old three\n");
		MockFileSystem.File.SetAttributes(locked, FileAttributes.ReadOnly);

		Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
			MergedContentWriter.WriteToAll(MockFileSystem, [first, locked, last], "merged\n", Encoding.UTF8));

		Assert.AreEqual("old one\r\n", MockFileSystem.File.ReadAllText(first));
		Assert.AreEqual("old two\n", MockFileSystem.File.ReadAllText(locked));
		Assert.AreEqual("old three\n", MockFileSystem.File.ReadAllText(last));
	}
}
