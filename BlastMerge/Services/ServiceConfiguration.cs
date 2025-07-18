// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using ktsu.BlastMerge.Contracts;
using ktsu.FileSystemProvider;
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

		// Utility services
		services.AddSingleton<IDiffPlexHelper, DiffPlexHelper>();
		services.AddSingleton<IWhitespaceVisualizer, WhitespaceVisualizer>();
		services.AddSingleton<ICharacterLevelDiffer, CharacterLevelDiffer>();

		// Persistence services (temporary simplified setup)
		// Note: Full persistence provider setup needs to be implemented
		// services.AddSingleton<BlastMergePersistenceService>();
		// services.AddSingleton<AppDataBatchManager>();
		// services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();
		// services.AddSingleton<IBatchConfigurationService, BatchConfigurationService>();
		// services.AddSingleton<IInputHistoryService, InputHistoryService>();
		// services.AddSingleton<IRecentBatchService, RecentBatchService>();

		// File operations services
		services.AddTransient<FileHasher>();
		services.AddTransient<FileFinder>();
		services.AddTransient<FileDiffer>();
		services.AddTransient<AsyncFileDiffer>();
		services.AddTransient<DiffPlexDiffer>();
		services.AddTransient<SecureTempFileHelper>();
		services.AddTransient<BlockMerger>();
		services.AddTransient<IterativeMergeOrchestrator>();
		services.AddTransient<BatchProcessor>();

		// Core application services
		services.AddSingleton<ApplicationService>();

		return services;
	}
}
