namespace Phantonia.Historia;

public readonly struct StoryEdge(int toVertex, int fromVertex, bool isWeak)
{
    public long ToVertex { get; } = toVertex;

    public long FromVertex { get; } = fromVertex;

    public bool IsWeak { get; } = isWeak;
}
