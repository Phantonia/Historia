namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public abstract record ExpressionNode() : SyntaxNode
{
    public abstract bool IsConstant { get; }
}
