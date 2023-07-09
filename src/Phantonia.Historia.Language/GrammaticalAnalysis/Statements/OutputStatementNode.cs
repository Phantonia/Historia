using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record OutputStatementNode : StatementNode
{
    public OutputStatementNode() { }

    public required ExpressionNode Expression { get; init; }
}
