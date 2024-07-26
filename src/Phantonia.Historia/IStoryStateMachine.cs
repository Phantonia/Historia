using System.Collections.Generic;

namespace Phantonia.Historia;

public interface IStoryStateMachine
{
    bool NotStartedStory { get; }

    bool FinishedStory { get; }

    object? Output { get; }

    IReadOnlyList<object?> Options { get; }

    bool TryContinue();

    bool TryContinueWithOption(int option);

    IStorySnapshot CreateSnapshot();
}

public interface IStoryStateMachine<out TOutput, out TOption> : IStoryStateMachine
{
    new TOutput? Output { get; }

    new IReadOnlyList<TOption> Options { get; }

    new IStorySnapshot<TOutput, TOption> CreateSnapshot();
}
