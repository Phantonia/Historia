namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowEdge
{
    public static FlowEdge CreateTo(int toVertex) => new()
    {
        ToVertex = toVertex,
        IsWeak = false,
    };

    public static FlowEdge CreateWeakTo(int toVertex) => new()
    {
        ToVertex = toVertex,
        IsWeak = true,
    };

    public int ToVertex { get; init; }

    public bool IsWeak { get; init; }
}
