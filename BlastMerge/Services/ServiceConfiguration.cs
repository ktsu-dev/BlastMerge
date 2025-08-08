// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;
using ktsu.PersistenceProvider;
using ktsu.SerializationProvider;
using ktsu.UniversalSerializer;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configures dependency injection services for BlastMerge.
/// </summary>
public static class ServiceConfiguration
{
	/// <summary>
	/// Configures all BlastMerge services for dependency injection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The configured service collection.</returns>
	public static IServiceCollection ConfigureBlastMergeServices(this IServiceCollection services)
	{
		// File system abstraction using ktsu.FileSystemProvider
		services.AddFileSystemProvider();

		// Environment abstraction
		services.AddSingleton<IEnvironmentProvider, EnvironmentProvider>();

		// Serialization provider setup
		services.AddSingleton<ISerializationProvider, UniversalSerializationProvider>();

		// Persistence provider setup
		services.AddSingleton<IPersistenceProvider<string>>(serviceProvider =>
		{
			ISerializationProvider serializationProvider = serviceProvider.GetRequiredService<ISerializationProvider>();
			IFileSystemProvider fileSystemProvider = serviceProvider.GetRequiredService<IFileSystemProvider>();

			// Use memory persistence provider for now (can be replaced with file-based later)
			return new AppDataPersistenceProvider<string>(fileSystemProvider, serializationProvider, nameof(BlastMerge));
		});

		// Initialize BlastMergeAppData with persistence provider
		services.AddSingleton(serviceProvider =>
		{
			IPersistenceProvider<string> persistenceProvider = serviceProvider.GetRequiredService<IPersistenceProvider<string>>();
			BlastMergeAppData.Initialize(persistenceProvider);
			return BlastMergeAppData.Get();
		});

		// Utility services
		services.AddSingleton<IDiffPlexHelper, DiffPlexHelper>();
		services.AddSingleton<IWhitespaceVisualizer, WhitespaceVisualizer>();
		services.AddSingleton<ICharacterLevelDiffer, CharacterLevelDiffer>();

		// Persistence-based services
		services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();
		services.AddSingleton<IBatchConfigurationService, BatchConfigurationService>();
		services.AddSingleton<IInputHistoryService, InputHistoryService>();
		services.AddSingleton<IRecentBatchService, RecentBatchService>();

		// File operations services
		services.AddTransient<FileHasher>();
		services.AddTransient<FileFinder>();
		services.AddTransient<FileDiffer>();
		services.AddTransient<AsyncFileDiffer>();
		services.AddTransient<DiffPlexDiffer>();
		services.AddTransient<SecureTempFileHelper>();
		services.AddTransient<BlockMerger>();

		// Batch processing services
		services.AddTransient<BatchProcessor>();
		services.AddTransient<IterativeMergeOrchestrator>();

		return services;
	}
}
