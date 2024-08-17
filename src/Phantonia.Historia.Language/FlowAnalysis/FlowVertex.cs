using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Diagnostics;

namespace Phantonia.Historia.Language.FlowAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct FlowVertex
{
    public required StatementNode AssociatedStatement { get; init; }

    public required int Index { get; init; }

    public bool IsVisible => Kind is FlowVertexKind.Visible;

    public bool IsStory => Kind is FlowVertexKind.Visible or FlowVertexKind.Invisible;

    public required FlowVertexKind Kind { get; init; }

    private string GetDebuggerDisplay()
        => $"{Kind switch
        {
            FlowVertexKind.Visible => "visible",
            FlowVertexKind.Invisible => "invisible",
            FlowVertexKind.PurelySemantic => "purely semantic",
            _ => "",
        }} flow vertex w/ statement ({AssociatedStatement.GetDebuggerDisplay()}) @ index {Index}";
}
