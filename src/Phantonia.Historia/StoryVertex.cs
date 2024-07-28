using System.Collections.Immutable;

namespace Phantonia.Historia;

public readonly struct StoryVertex<TOutput, TOption>
{
    public StoryVertex(int index, TOutput output, ReadOnlyList<TOption> options, ReadOnlyList<StoryEdge> outgoingEdges, ReadOnlyList<StoryEdge> incomingEdges)
    {
        Index = index;
        Output = output;
        Options = options;
        OutgoingEdges = outgoingEdges;
        IncomingEdges = incomingEdges;
    }

    public int Index { get; }

    public TOutput Output { get; }

    public ReadOnlyList<TOption> Options { get; }

    public ReadOnlyList<StoryEdge> OutgoingEdges { get; }

    public ReadOnlyList<StoryEdge> IncomingEdges { get; }
}
