namespace Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

public sealed record IntegerLiteralExpressionNode : ExpressionNode
{
    public IntegerLiteralExpressionNode() { }

    public required int Value { get; init; }
}
