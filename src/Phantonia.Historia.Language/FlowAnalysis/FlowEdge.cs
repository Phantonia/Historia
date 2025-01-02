namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowEdge
{
    public static FlowEdge CreateStrongTo(long toVertex) => new()
    {
        ToVertex = toVertex,
        Kind = FlowEdgeKind.Strong,
    };

    public static FlowEdge CreateWeakTo(long toVertex) => new()
    {
        ToVertex = toVertex,
        Kind = FlowEdgeKind.Weak,
    };

    public static FlowEdge CreatePurelySemanticTo(long toVertex) => new()
    {
        ToVertex = toVertex,
        Kind = FlowEdgeKind.Semantic,
    };

    public long ToVertex { get; init; }

    public FlowEdgeKind Kind { get; init; }

    public bool IsPurelySemantic => IsSemantic && !IsStory;

    public bool IsSemantic => (Kind & FlowEdgeKind.Semantic) != 0;

    public bool IsStory => (Kind & FlowEdgeKind.Story) != 0;

    public bool IsStrong => IsStory && IsSemantic;

    public bool IsWeak => IsStory && !IsSemantic;
}
