namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowEdge
{
    public static FlowEdge CreateTo(int toVertex) => new()
    {
        ToVertex = toVertex,
        Weak = false,
    };

    public static FlowEdge CreateWeakTo(int toVertex) => new()
    {
        ToVertex = toVertex,
        Weak = true,
    };

    public int ToVertex { get; init; }

    public bool Weak { get; init; }
}
