using System.Collections.Generic;

namespace Phantonia.Historia;

public interface IStory
{
    bool NotStartedStory { get; }

    bool FinishedStory { get; }

    object? Output { get; }

    IReadOnlyList<object?> Options { get; }

    bool TryContinue();

    bool TryContinueWithOption(int option);
}

public interface IStory<out TOutput, out TOption> : IStory
{
    new TOutput? Output { get; }

    new IReadOnlyList<TOption> Options { get; }
}
