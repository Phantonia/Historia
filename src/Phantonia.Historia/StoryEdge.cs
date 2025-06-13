namespace Phantonia.Historia;

/// <summary>
/// Represents a directed edge in the story graph.
/// </summary>
/// <param name="toVertex">The end point of the edge.</param>
/// <param name="fromVertex">The start point of the edge.</param>
/// <param name="isWeak">Whether this edge points up and introduces a cycle.</param>
public readonly struct StoryEdge(uint toVertex, uint fromVertex, bool isWeak)
{
    /// <summary>
    /// The end point of the edge.
    /// </summary>
    public uint ToVertex { get; } = toVertex;

    /// <summary>
    /// The start point of the edge.
    /// </summary>
    public uint FromVertex { get; } = fromVertex;

    /// <summary>
    /// Whether this edge points up and introduces a cycle.
    /// </summary>
    public bool IsWeak { get; } = isWeak;
}
