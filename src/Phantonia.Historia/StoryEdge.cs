namespace Phantonia.Historia;

public readonly struct StoryEdge
{
    public StoryEdge(int toVertex, int fromVertex, bool isWeak)
    {
        ToVertex = toVertex;
        FromVertex = fromVertex;
        IsWeak = isWeak;
    }

    public int ToVertex { get; }

    public int FromVertex { get; }

    public bool IsWeak { get; }
}
