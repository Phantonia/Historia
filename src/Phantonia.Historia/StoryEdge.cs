namespace Phantonia.Historia;

public readonly struct StoryEdge(int toVertex, int fromVertex, bool isWeak)
{
    public int ToVertex { get; } = toVertex;

    public int FromVertex { get; } = fromVertex;

    public bool IsWeak { get; } = isWeak;
}
