using System.Collections.Generic;

namespace Phantonia.Historia;

/// <summary>
/// Represents an object that can progress through a story.
/// </summary>
public interface IStoryStateMachine
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
    /// Attempts to progress the story.
    /// </summary>
    /// <returns>Whether the story could be progressed.</returns>
    bool TryContinue();

    /// <summary>
    /// Attempts to progress the story with an option from <see cref="Options"/>.
    /// </summary>
    /// <param name="option">An index into <see cref="Options"/>.</param>
    /// <returns>Whether the story could be progressed with the given option.</returns>
    bool TryContinueWithOption(int option);

    /// <summary>
    /// Creates a <see cref="IStorySnapshot"/> at the current state in the story.
    /// </summary>
    /// <returns>A snapshot.</returns>
    IStorySnapshot CreateSnapshot();
}

/// <summary>
/// Represents an object that can progress through a story.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TOption">The option type.</typeparam>
public interface IStoryStateMachine<out TOutput, out TOption> : IStoryStateMachine
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
    /// Creates a <see cref="IStorySnapshot{TOutput,TOption}"/> at the current state in the story.
    /// </summary>
    /// <returns>A snapshot.</returns>
    new IStorySnapshot<TOutput, TOption> CreateSnapshot();
}
