namespace Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

public sealed record StringLiteralExpressionNode : ExpressionNode
{
    public StringLiteralExpressionNode() { }

    public required string StringLiteral { get; init; }
}
