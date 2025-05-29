using System.Collections.Generic;

namespace Phantonia.Historia;

/// <summary>
/// Represents an immutable snapshot of a state in a story.
/// </summary>
public interface IStorySnapshot
{
    /// <summary>
    /// Whether the story has not yet started.
    /// </summary>
    bool NotStartedStory { get; }

    /// <summary>
    /// Whether the story is finished.
    /// </summary>
    bool FinishedStory { get; }

    /// <summary>
    /// Whether calling <see cref="TryContinue"/> will work.
    /// </summary>
    bool CanContinueWithoutOption { get; }

    /// <summary>
    /// The output value.
    /// </summary>
    object? Output { get; }

    /// <summary>
    /// The current options.
    /// </summary>
    IReadOnlyList<object?> Options { get; }

    /// <summary>
    /// Attempts to progress the story and create a new snapshot at the new state.
    /// </summary>
    /// <returns>A new snapshot at the new state, or null if that didn't work.</returns>
    IStorySnapshot? TryContinue();

    /// <summary>
    /// Attempts to progress the story with an option from <see cref="Options"/> and create a new snapshot at the new state.
    /// </summary>
    /// <param name="option">An index into <see cref="Options"/>.</param>
    /// <returns>A new snapshot at the new state, or null if that didn't work.</returns>
    IStorySnapshot? TryContinueWithOption(int option);
}

/// <summary>
/// Represents an immutable snapshot of a state in a story.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TOption">The option type.</typeparam>
public interface IStorySnapshot<out TOutput, out TOption> : IStorySnapshot
{
    /// <summary>
    /// The output value.
    /// </summary>
    new TOutput? Output { get; }

    /// <summary>
    /// The current options.
    /// </summary>
    new IReadOnlyList<TOption> Options { get; }

    /// <summary>
    /// Attempts to progress the story and create a new snapshot at the new state.
    /// </summary>
    /// <returns>A new snapshot at the new state, or null if that didn't work.</returns>
    new IStorySnapshot<TOutput, TOption>? TryContinue();

    /// <summary>
    /// Attempts to progress the story with an option from <see cref="Options"/> and create a new snapshot at the new state.
    /// </summary>
    /// <param name="option">An index into <see cref="Options"/>.</param>
    /// <returns>A new snapshot at the new state, or null if that didn't work.</returns>
    new IStorySnapshot<TOutput, TOption>? TryContinueWithOption(int option);
}
