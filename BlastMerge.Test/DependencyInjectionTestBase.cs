// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using ktsu.FileSystemProvider;
using ktsu.PersistenceProvider;
using ktsu.SerializationProvider;
using ktsu.UniversalSerializer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base class for tests that use dependency injection with in-memory persistence
/// </summary>
public abstract class DependencyInjectionTestBase
{
	/// <summary>
	/// The service provider for dependency injection
	/// </summary>
	protected ServiceProvider ServiceProvider { get; private set; } = null!;

	/// <summary>
	/// The mock file system instance used for testing
	/// </summary>
	protected MockFileSystem MockFileSystem { get; private set; } = null!;

	/// <summary>
	/// The root directory for the mock file system
	/// </summary>
	protected string TestDirectory { get; private set; } = null!;

	/// <summary>
	/// A unique identifier for this test instance
	/// </summary>
	protected string TestId { get; } = Guid.NewGuid().ToString("N");

	/// <summary>
	/// Initializes the dependency injection container with test services
	/// </summary>
	[TestInitialize]
	public virtual void SetUp()
	{
		// Create unique test directory per test
		TestDirectory = $@"C:\mock-test-dir-{TestId}";

		// Create a fresh mock filesystem instance for this test
		MockFileSystem = new MockFileSystem();
		MockFileSystem.Directory.CreateDirectory(TestDirectory);

		// Configure services
		ServiceCollection services = new();

		// Register mock file system provider that uses our MockFileSystem
		services.AddSingleton<IFileSystemProvider>(serviceProvider =>
		{
			FileSystemProvider provider = new();
			provider.SetFileSystemFactory(() => MockFileSystem);
			return provider;
		});

		// Register serialization provider
		services.AddSingleton<ISerializationProvider, UniversalSerializationProvider>();

		// Register in-memory persistence provider for testing
		services.AddSingleton<IPersistenceProvider<string>>(serviceProvider =>
		{
			ISerializationProvider serializationProvider = serviceProvider.GetRequiredService<ISerializationProvider>();
			return new MemoryPersistenceProvider<string>(serializationProvider);
		});

		// Register BlastMergeAppData with persistence provider
		services.AddSingleton<BlastMergeAppData>(serviceProvider =>
		{
			IPersistenceProvider<string> persistenceProvider = serviceProvider.GetRequiredService<IPersistenceProvider<string>>();
			BlastMergeAppData.Initialize(persistenceProvider);
			return BlastMergeAppData.Get();
		});

		// Register core services with FileSystemProvider dependency
		services.AddSingleton<FileHasher>();
		services.AddSingleton<DiffPlexDiffer>();
		services.AddSingleton<FileFinder>();
		services.AddSingleton<FileDiffer>();
		services.AddSingleton<WhitespaceVisualizer>();
		services.AddSingleton<CharacterLevelDiffer>();
		services.AddSingleton<DiffPlexHelper>();
		services.AddSingleton<BlockMerger>();
		services.AddSingleton<AsyncFileDiffer>();
		services.AddSingleton<BatchProcessor>();
		services.AddSingleton<IterativeMergeOrchestrator>();

		// Add any additional test-specific services
		ConfigureTestServices(services);

		// Build the service provider
		ServiceProvider = services.BuildServiceProvider();

		// Initialize test data
		InitializeTestData();
	}

	/// <summary>
	/// Clean up after tests
	/// </summary>
	[TestCleanup]
	public virtual void Cleanup()
	{
		ServiceProvider?.Dispose();
	}

	/// <summary>
	/// Override this method to configure additional test-specific services
	/// </summary>
	/// <param name="services">The service collection to configure</param>
	protected virtual void ConfigureTestServices(ServiceCollection services)
	{
		// To be overridden by derived classes
	}

	/// <summary>
	/// Override this method to set up your specific test data
	/// </summary>
	protected virtual void InitializeTestData()
	{
		// To be overridden by derived classes
	}

	/// <summary>
	/// Gets a service from the dependency injection container
	/// </summary>
	/// <typeparam name="T">The service type</typeparam>
	/// <returns>The service instance</returns>
	protected T GetService<T>() where T : notnull
	{
		return ServiceProvider.GetRequiredService<T>();
	}

	/// <summary>
	/// Creates a file in the mock filesystem
	/// </summary>
	/// <param name="relativePath">Path relative to the test directory</param>
	/// <param name="content">Content to write to the file</param>
	/// <returns>Full path to the created file</returns>
	protected string CreateFile(string relativePath, string content)
	{
		string fullPath = MockFileSystem.Path.Combine(TestDirectory, relativePath);
		string? directory = MockFileSystem.Path.GetDirectoryName(fullPath);

		if (!string.IsNullOrEmpty(directory) && !MockFileSystem.Directory.Exists(directory))
		{
			MockFileSystem.Directory.CreateDirectory(directory);
		}

		MockFileSystem.File.WriteAllText(fullPath, content);
		return fullPath;
	}

	/// <summary>
	/// Creates a directory in the mock filesystem
	/// </summary>
	/// <param name="relativePath">Path relative to the test directory</param>
	/// <returns>Full path to the created directory</returns>
	protected string CreateDirectory(string relativePath)
	{
		string fullPath = MockFileSystem.Path.Combine(TestDirectory, relativePath);
		MockFileSystem.Directory.CreateDirectory(fullPath);
		return fullPath;
	}
}
