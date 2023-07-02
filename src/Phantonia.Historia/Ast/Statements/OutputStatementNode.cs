using Phantonia.Historia.Language.Ast.Expressions;

namespace Phantonia.Historia.Language.Ast.Statements;

public sealed record OutputStatementNode : StatementNode
{
    public OutputStatementNode() { }

    public required ExpressionNode Expression { get; init; }
}
