// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using ktsu.BlastMerge.Contracts;
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
		return services.AddFileSystemProvider()
			.AddPersistenceProvider()
			.AddTransient<FileHasher>()
			.AddTransient<FileFinder>()
			.AddTransient<FileDiffer>()
			.AddTransient<AsyncFileDiffer>()
			.AddTransient<DiffPlexDiffer>()
			.AddTransient<BlockMerger>()
			.AddTransient<BatchProcessor>()
			.AddTransient<IterativeMergeOrchestrator>()
			.AddSingleton<IAppDataService, AppDataService>()
			.AddSingleton<IDiffPlexHelper, DiffPlexHelper>()
			.AddSingleton<IWhitespaceVisualizer, WhitespaceVisualizer>()
			.AddSingleton<ICharacterLevelDiffer, CharacterLevelDiffer>()
			.AddSingleton<IEnvironmentProvider, EnvironmentProvider>()
			.AddSingleton<ISerializationProvider, UniversalSerializationProvider>();
	}

	/// <summary>
	/// Adds the persistence provider to the service collection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The configured service collection.</returns>
	public static IServiceCollection AddPersistenceProvider(this IServiceCollection services)
	{
		return services.AddSingleton<IPersistenceProvider<string>>(serviceProvider =>
		{
			ISerializationProvider serializationProvider = serviceProvider.GetRequiredService<ISerializationProvider>();
			IFileSystemProvider fileSystemProvider = serviceProvider.GetRequiredService<IFileSystemProvider>();
			return new AppDataPersistenceProvider<string>(fileSystemProvider, serializationProvider, nameof(BlastMerge));
		});
	}
}
