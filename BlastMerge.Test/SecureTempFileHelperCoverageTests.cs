// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

/// <summary>
/// Covers the failure handling of <see cref="SecureTempFileHelper"/>: an unwritable or inaccessible
/// temp directory, name collisions that never clear, and clean-up that must never throw.
/// </summary>
[TestClass]
public class SecureTempFileHelperCoverageTests
{
	private MockFileSystem backing = null!;
	private Mock<IFileSystem> fileSystem = null!;
	private Mock<IFile> file = null!;
	private Mock<IDirectory> directory = null!;
	private string tempPath = null!;

	/// <summary>
	/// Builds a file system double whose path handling and temp directory are real, so each test
	/// only has to say which file or directory operation misbehaves.
	/// </summary>
	[TestInitialize]
	public void SetUp()
	{
		backing = new MockFileSystem();
		tempPath = backing.Path.GetTempPath();
		backing.Directory.CreateDirectory(tempPath);

		Mock<IPath> path = new();
		path.Setup(p => p.GetTempPath()).Returns(tempPath);
		path.Setup(p => p.GetRandomFileName()).Returns(backing.Path.GetRandomFileName);
		path.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns((string a, string b) => backing.Path.Join(a, b));
		path.Setup(p => p.ChangeExtension(It.IsAny<string>(), It.IsAny<string>())).Returns((string p, string e) => backing.Path.ChangeExtension(p, e)!);

		file = new Mock<IFile>();
		file.Setup(f => f.Create(It.IsAny<string>())).Returns((string p) => backing.File.Create(p));
		file.Setup(f => f.Delete(It.IsAny<string>())).Callback((string p) => backing.File.Delete(p));

		directory = new Mock<IDirectory>();
		directory.Setup(d => d.Exists(It.IsAny<string>())).Returns((string p) => backing.Directory.Exists(p));

		fileSystem = new Mock<IFileSystem>();
		fileSystem.Setup(f => f.Path).Returns(path.Object);
		fileSystem.Setup(f => f.File).Returns(file.Object);
		fileSystem.Setup(f => f.Directory).Returns(directory.Object);
	}

	/// <summary>
	/// A temp directory that does not exist yet is created before the file is.
	/// </summary>
	[TestMethod]
	public void CreateTempFile_CreatesMissingTempDirectory()
	{
		MockFileSystem empty = new(new Dictionary<string, MockFileData>(), new MockFileSystemOptions { CreateDefaultTempDir = false });
		string emptyTemp = empty.Path.GetTempPath();
		Assert.IsFalse(empty.Directory.Exists(emptyTemp));

		string created = SecureTempFileHelper.CreateTempFile(".dat", empty);

		Assert.IsTrue(empty.Directory.Exists(emptyTemp));
		Assert.IsTrue(empty.File.Exists(created));
		Assert.AreEqual(".dat", empty.Path.GetExtension(created));
	}

	/// <summary>
	/// A temp directory that refuses writes is reported as not writable.
	/// </summary>
	[TestMethod]
	public void CreateTempFile_UnwritableTempDirectory_ThrowsIOException()
	{
		UnauthorizedAccessException denied = new("denied");
		file.Setup(f => f.Create(It.IsAny<string>())).Throws(denied);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempFile(fileSystem.Object));

		Assert.Contains("is not writable due to insufficient permissions", exception.Message);
		Assert.Contains(tempPath, exception.Message);
		Assert.AreSame(denied, exception.InnerException);
	}

	/// <summary>
	/// A temp directory that fails with an I/O error is reported as not accessible.
	/// </summary>
	[TestMethod]
	public void CreateTempDirectory_InaccessibleTempDirectory_ThrowsIOException()
	{
		IOException failure = new("disk gone");
		file.Setup(f => f.Create(It.IsAny<string>())).Throws(failure);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempDirectory(fileSystem.Object));

		Assert.Contains("is not accessible for secure file creation", exception.Message);
		Assert.AreSame(failure, exception.InnerException);
	}

	/// <summary>
	/// When every candidate name already exists the helper gives up after its retry limit.
	/// </summary>
	[TestMethod]
	public void CreateTempFile_PersistentCollision_GivesUpAfterRetries()
	{
		int creates = 0;
		file.Setup(f => f.Create(It.IsAny<string>())).Returns((string p) =>
		{
			creates++;
			return creates == 1 ? backing.File.Create(p) : throw new IOException("exists");
		});
		file.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempFile(".dat", fileSystem.Object));

		Assert.AreEqual("Unable to create a unique temporary file with extension '.dat' after 100 attempts.", exception.Message);
		Assert.AreEqual(101, creates);
	}

	/// <summary>
	/// An I/O failure that is not a collision is not retried.
	/// </summary>
	[TestMethod]
	public void CreateTempFile_FailureWithoutCollision_Propagates()
	{
		int creates = 0;
		file.Setup(f => f.Create(It.IsAny<string>())).Returns((string p) =>
		{
			creates++;
			return creates == 1 ? backing.File.Create(p) : throw new IOException("device error");
		});
		file.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempFile(fileSystem.Object));

		Assert.AreEqual("device error", exception.Message);
		Assert.AreEqual(2, creates);
	}

	/// <summary>
	/// When every candidate directory already exists the helper gives up after its retry limit.
	/// </summary>
	[TestMethod]
	public void CreateTempDirectory_EveryNameTaken_GivesUpAfterRetries()
	{
		directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempDirectory(fileSystem.Object));

		Assert.AreEqual("Unable to create a unique temporary directory after 100 attempts.", exception.Message);
		directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never());
	}

	/// <summary>
	/// I/O and access failures while creating a directory are retried until the limit.
	/// </summary>
	/// <param name="exceptionType">The exception the directory creation throws.</param>
	[TestMethod]
	[DataRow(typeof(IOException))]
	[DataRow(typeof(UnauthorizedAccessException))]
	public void CreateTempDirectory_CreationFailures_AreRetried(Type exceptionType)
	{
		directory.Setup(d => d.CreateDirectory(It.IsAny<string>())).Throws((Exception)Activator.CreateInstance(exceptionType)!);

		IOException exception = Assert.ThrowsExactly<IOException>(() => SecureTempFileHelper.CreateTempDirectory(fileSystem.Object));

		Assert.AreEqual("Unable to create a unique temporary directory after 100 attempts.", exception.Message);
		directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Exactly(100));
	}

	/// <summary>
	/// A failure to delete a temp file is swallowed.
	/// </summary>
	/// <param name="exceptionType">The exception the delete throws.</param>
	[TestMethod]
	[DataRow(typeof(IOException))]
	[DataRow(typeof(UnauthorizedAccessException))]
	[DataRow(typeof(ArgumentException))]
	public void SafeDeleteTempFile_SwallowsCleanupFailures(Type exceptionType)
	{
		file.Setup(f => f.Exists("victim")).Returns(true);
		file.Setup(f => f.Delete("victim")).Throws((Exception)Activator.CreateInstance(exceptionType)!);

		SecureTempFileHelper.SafeDeleteTempFile("victim", fileSystem.Object);

		file.Verify(f => f.Delete("victim"), Times.Once());
	}

	/// <summary>
	/// A failure to delete a temp directory is swallowed.
	/// </summary>
	/// <param name="exceptionType">The exception the delete throws.</param>
	[TestMethod]
	[DataRow(typeof(IOException))]
	[DataRow(typeof(UnauthorizedAccessException))]
	[DataRow(typeof(ArgumentException))]
	public void SafeDeleteTempDirectory_SwallowsCleanupFailures(Type exceptionType)
	{
		directory.Setup(d => d.Exists("victim")).Returns(true);
		directory.Setup(d => d.Delete("victim", true)).Throws((Exception)Activator.CreateInstance(exceptionType)!);

		SecureTempFileHelper.SafeDeleteTempDirectory("victim", fileSystem.Object);

		directory.Verify(d => d.Delete("victim", true), Times.Once());
	}

	/// <summary>
	/// A null or empty directory path is ignored without touching the file system.
	/// </summary>
	[TestMethod]
	public void SafeDeleteTempDirectory_EmptyPath_DoesNothing()
	{
		Mock<IFileSystem> strict = new(MockBehavior.Strict);

		SecureTempFileHelper.SafeDeleteTempDirectory(null, strict.Object);
		SecureTempFileHelper.SafeDeleteTempDirectory(string.Empty, strict.Object);

		strict.VerifyNoOtherCalls();
	}

	/// <summary>
	/// An existing temp directory is deleted together with its contents.
	/// </summary>
	[TestMethod]
	public void SafeDeleteTempDirectory_DeletesRecursively()
	{
		string created = SecureTempFileHelper.CreateTempDirectory(backing);
		backing.File.WriteAllText(backing.Path.Join(created, "inner.txt"), "x");

		SecureTempFileHelper.SafeDeleteTempDirectory(created, backing);

		Assert.IsFalse(backing.Directory.Exists(created));
	}
}
