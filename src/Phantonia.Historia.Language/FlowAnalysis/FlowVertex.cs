using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.FlowAnalysis;

public readonly record struct FlowVertex
{
    public required StatementNode AssociatedStatement { get; init; }

    public required int Index { get; init; }
}
