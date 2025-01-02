namespace Phantonia.Historia;

public readonly struct StoryEdge(long toVertex, long fromVertex, bool isWeak)
{
    public long ToVertex { get; } = toVertex;

    public long FromVertex { get; } = fromVertex;

    public bool IsWeak { get; } = isWeak;
}
