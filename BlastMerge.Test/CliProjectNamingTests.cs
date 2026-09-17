// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins the console entry point to the org-wide <c>.Cli</c> suffix convention.
/// </summary>
/// <remarks>
/// The assembly name and root namespace both derive from the project folder, so renaming the
/// folder back to <c>BlastMerge.ConsoleApp</c> (or to any other spelling) fails these assertions
/// without anyone having to notice the rename in review.
/// </remarks>
[TestClass]
public class CliProjectNamingTests
{
	/// <summary>
	/// The assembly that carries the console entry point.
	/// </summary>
	private static Assembly CliAssembly => typeof(Cli.Program).Assembly;

	/// <summary>
	/// The console entry point assembly is named for the project folder, so this pins the folder name too.
	/// </summary>
	[TestMethod]
	public void CliAssemblyIsNamedWithTheCliSuffix() =>
		Assert.AreEqual("ktsu.BlastMerge.Cli", CliAssembly.GetName().Name);

	/// <summary>
	/// The root namespace follows the same derivation, so it must carry the same suffix.
	/// </summary>
	[TestMethod]
	public void CliEntryPointLivesInTheCliNamespace() =>
		Assert.AreEqual("ktsu.BlastMerge.Cli", typeof(Cli.Program).Namespace);

	/// <summary>
	/// Nothing in the entry point assembly may reuse the retired <c>.ConsoleApp</c> spelling.
	/// </summary>
	[TestMethod]
	public void NoTypeUsesTheRetiredConsoleAppNamespace()
	{
		foreach (Type type in CliAssembly.GetTypes())
		{
			Assert.IsFalse(
				type.Namespace?.StartsWith("ktsu.BlastMerge.ConsoleApp", StringComparison.Ordinal) ?? false,
				$"{type.FullName} still uses the retired ktsu.BlastMerge.ConsoleApp namespace.");
		}
	}
}
