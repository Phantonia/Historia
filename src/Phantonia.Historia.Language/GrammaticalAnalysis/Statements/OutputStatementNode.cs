using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record OutputStatementNode : StatementNode, IOutputStatementNode
{
    public OutputStatementNode() { }

    public required ExpressionNode OutputExpression { get; init; }
}
