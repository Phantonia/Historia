using System.Collections.Generic;

namespace Phantonia.Historia;

public interface IStorySnapshot
{
    bool NotStartedStory { get; }

    bool FinishedStory { get; }

    bool CanContinueWithoutOption { get; }

    object? Output { get; }

    IReadOnlyList<object?> Options { get; }

    IStorySnapshot? TryContinue();

    IStorySnapshot? TryContinueWithOption(int option);
}

public interface IStorySnapshot<out TOutput, out TOption> : IStorySnapshot
{
    new TOutput? Output { get; }

    new IReadOnlyList<TOption> Options { get; }

    new IStorySnapshot<TOutput, TOption>? TryContinue();

    new IStorySnapshot<TOutput, TOption>? TryContinueWithOption(int option);
}
