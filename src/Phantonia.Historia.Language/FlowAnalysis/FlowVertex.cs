using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowVertex
{
    public required StatementNode AssociatedStatement { get; init; }

    public required int Index { get; init; }

    public required bool IsVisible { get; init; }
}
