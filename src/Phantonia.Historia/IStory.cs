using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia;

public interface IStory
{
    bool FinishedStory { get; }

    object? Output { get; }

    ImmutableArray<object?> Options { get; }

    bool TryContinue();

    bool TryContinueWithOption(int option);
}

public interface IStory<TOutput, TOption> : IStory
{
    new TOutput Output { get; }

    new ImmutableArray<TOption> Options { get; }

    object? IStory.Output => Output;

    ImmutableArray<object?> IStory.Options => Options.Select(o => (object?)o).ToImmutableArray();
}
