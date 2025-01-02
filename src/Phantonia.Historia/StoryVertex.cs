namespace Phantonia.Historia;

public readonly struct StoryVertex<TOutput, TOption>(long index, TOutput output, ReadOnlyList<TOption> options, ReadOnlyList<StoryEdge> outgoingEdges, ReadOnlyList<StoryEdge> incomingEdges)
{
    public long Index { get; } = index;

    public TOutput Output { get; } = output;

    public ReadOnlyList<TOption> Options { get; } = options;

    public ReadOnlyList<StoryEdge> OutgoingEdges { get; } = outgoingEdges;

    public ReadOnlyList<StoryEdge> IncomingEdges { get; } = incomingEdges;
}
