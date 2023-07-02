namespace Phantonia.Historia.Language.Ast.Expressions;

public sealed record IntegerLiteralExpressionNode : ExpressionNode
{
    public IntegerLiteralExpressionNode() { }

    public required int Value { get; init; }
}
