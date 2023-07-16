namespace Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

public sealed record ArgumentNode : SyntaxNode
{
    public ArgumentNode() { }

    public required ExpressionNode Expression { get; init; }

    public string? PropertyName { get; init; }
}
