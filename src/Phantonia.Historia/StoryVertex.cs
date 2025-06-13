namespace Phantonia.Historia;

/// <summary>
/// Represents a vertex in the story graph.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TOption">The option type.</typeparam>
/// <param name="index">The index of this vertex.</param>
/// <param name="output">The output value of this vertex.</param>
/// <param name="options">The options of this vertex.</param>
/// <param name="outgoingEdges">The edges eminating from this vertex.</param>
/// <param name="incomingEdges">The edges ending in this vertex.</param>
public readonly struct StoryVertex<TOutput, TOption>(uint index, TOutput output, ReadOnlyList<TOption> options, ReadOnlyList<StoryEdge> outgoingEdges, ReadOnlyList<StoryEdge> incomingEdges)
{
    /// <summary>
    /// The index of this vertex.
    /// </summary>
    public uint Index { get; } = index;

    /// <summary>
    /// The output value of this vertex.
    /// </summary>
    public TOutput Output { get; } = output;

    /// <summary>
    /// The options of this vertex.
    /// </summary>
    public ReadOnlyList<TOption> Options { get; } = options;

    /// <summary>
    /// The edges eminating from this vertex.
    /// </summary>
    public ReadOnlyList<StoryEdge> OutgoingEdges { get; } = outgoingEdges;

    /// <summary>
    /// The edges ending in this vertex.
    /// </summary>
    public ReadOnlyList<StoryEdge> IncomingEdges { get; } = incomingEdges;
}
