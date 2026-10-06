// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

/// <summary>
/// A console that renders through a <see cref="TestConsole"/> but reads its keystrokes from a script
/// that can also run actions between them.
/// </summary>
/// <remarks>
/// The CLI services decide what to operate on first and ask questions afterwards. An action queued
/// between two keystrokes runs at exactly the moment the service stops to wait for input, which is
/// how a test changes the files on disk after a service has looked at them and before it acts on
/// them, without any reliance on timing.
/// </remarks>
/// <param name="inner">The console that renders the output.</param>
internal sealed class ScriptedConsole(TestConsole inner) : IAnsiConsole
{
	private readonly ScriptedInput input = new();

	/// <inheritdoc/>
	public Profile Profile => inner.Profile;

	/// <inheritdoc/>
	public IAnsiConsoleCursor Cursor => inner.Cursor;

	/// <inheritdoc/>
	public IAnsiConsoleInput Input => input;

	/// <inheritdoc/>
	public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;

	/// <inheritdoc/>
	public RenderPipeline Pipeline => inner.Pipeline;

	/// <inheritdoc/>
	public void Clear(bool home) => inner.Clear(home);

	/// <inheritdoc/>
	public void Write(IRenderable renderable) => inner.Write(renderable);

	/// <summary>
	/// Queues a key press.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>This console, for chaining.</returns>
	public ScriptedConsole Key(ConsoleKey key)
	{
		input.Enqueue(new ConsoleKeyInfo((char)0, key, false, false, false));
		return this;
	}

	/// <summary>
	/// Queues a selection of the choice <paramref name="index"/> places below the first.
	/// </summary>
	/// <param name="index">The zero-based index of the choice.</param>
	/// <returns>This console, for chaining.</returns>
	public ScriptedConsole Select(int index)
	{
		for (int i = 0; i < index; i++)
		{
			Key(ConsoleKey.DownArrow);
		}

		return Key(ConsoleKey.Enter);
	}

	/// <summary>
	/// Queues typed text followed by Enter.
	/// </summary>
	/// <param name="text">The text to type.</param>
	/// <returns>This console, for chaining.</returns>
	public ScriptedConsole Line(string text)
	{
		foreach (char character in text)
		{
			input.Enqueue(new ConsoleKeyInfo(character, (ConsoleKey)character, false, false, false));
		}

		return Key(ConsoleKey.Enter);
	}

	/// <summary>
	/// Queues an action to run when the next key is read.
	/// </summary>
	/// <param name="action">The action.</param>
	/// <returns>This console, for chaining.</returns>
	public ScriptedConsole Then(Action action)
	{
		input.Enqueue(action);
		return this;
	}

	private sealed class ScriptedInput : IAnsiConsoleInput
	{
		private readonly Queue<object> script = new();

		public void Enqueue(object item) => script.Enqueue(item);

		public bool IsKeyAvailable() => script.Count > 0;

		public ConsoleKeyInfo? ReadKey(bool intercept)
		{
			while (script.Count > 0 && script.Peek() is Action action)
			{
				script.Dequeue();
				action();
			}

			if (script.Count == 0)
			{
				throw new InvalidOperationException("No input available.");
			}

			return (ConsoleKeyInfo)script.Dequeue();
		}

		public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
			Task.FromResult(ReadKey(intercept));
	}
}
