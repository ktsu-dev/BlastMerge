// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using Microsoft.Extensions.DependencyInjection;
using ktsu.FileSystemProvider;
using ktsu.PersistenceProvider;
using ktsu.SerializationProvider;
using ktsu.UniversalSerializer;

/// <summary>
/// Service registration for BlastMerge dependencies.
/// </summary>
public static class ServiceRegistration
{
	/// <summary>
	/// Registers BlastMerge services with the dependency injection container.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <param name="applicationName">The name of the application for AppData storage.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddBlastMergeServices(this IServiceCollection services, string applicationName = "BlastMerge")
	{
		// Register file system provider
		services.AddSingleton<IFileSystemProvider, global::ktsu.FileSystemProvider.FileSystemProvider>();

		// Register universal serialization provider
		services.AddSingleton<ISerializationProvider, UniversalSerializationProvider>();

		// Register persistence provider - uses AppData directory (primary storage)
		services.AddSingleton<IPersistenceProvider<string>>(serviceProvider =>
		{
			IFileSystemProvider fileSystemProvider = serviceProvider.GetRequiredService<IFileSystemProvider>();
			ISerializationProvider serializationProvider = serviceProvider.GetRequiredService<ISerializationProvider>();

			return new AppDataPersistenceProvider<string>(
				fileSystemProvider,
				serializationProvider,
				applicationName);
		});

		// TODO: Add additional persistence providers using keyed services when needed:
		// - TempPersistenceProvider for temporary session data
		// - MemoryPersistenceProvider for in-memory caching
		// Example: services.AddKeyedSingleton<IPersistenceProvider<string>>("temp", ...)

		// Register our persistence service
		services.AddSingleton<BlastMergePersistenceService>();

		// Register batch manager
		services.AddSingleton<AppDataBatchManager>();

		// Register core services with FileSystemProvider dependency
		services.AddSingleton<FileFinder>();
		services.AddSingleton<FileDiffer>();

		// Register the base application service
		services.AddSingleton<ApplicationService>();

		// Note: Many services are static utility classes and don't need DI registration:
		// - AsyncFileDiffer, BatchProcessor, BlockMerger, CharacterLevelDiffer, 
		// - DiffPlexDiffer, DiffPlexHelper, WhitespaceVisualizer, IterativeMergeOrchestrator

		return services;
	}
}
