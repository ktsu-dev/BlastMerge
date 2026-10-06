// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using ktsu.BlastMerge.Services;

/// <summary>
/// An application service for menu handler tests that records what it is asked to do instead of doing it.
/// </summary>
internal sealed class RecordingMenuApplicationService : ApplicationService
{
	/// <summary>
	/// Gets every call made to the service, in order, as <c>Method(argument, argument)</c>.
	/// </summary>
	public List<string> Calls { get; } = [];

	/// <inheritdoc/>
	public override void ProcessFiles(string directory, string fileName) =>
		Calls.Add($"{nameof(ProcessFiles)}({directory}, {fileName})");

	/// <inheritdoc/>
	public override void ProcessBatch(string directory, string batchName) =>
		Calls.Add($"{nameof(ProcessBatch)}({directory}, {batchName})");

	/// <inheritdoc/>
	public override void RunIterativeMerge(string directory, string fileName) =>
		Calls.Add($"{nameof(RunIterativeMerge)}({directory}, {fileName})");

	/// <inheritdoc/>
	public override void ListBatches() => Calls.Add($"{nameof(ListBatches)}()");

	/// <inheritdoc/>
	public override void StartInteractiveMode() => Calls.Add($"{nameof(StartInteractiveMode)}()");
}
