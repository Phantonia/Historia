using Phantonia.Historia.Language.Ast.Expressions;

namespace Phantonia.Historia.Language.Ast.Statements;

public sealed record OptionNode : SyntaxNode
{
    public OptionNode() { }

    public required StatementBodyNode Body { get; init; }

    public required ExpressionNode Expression { get; init; }
}
