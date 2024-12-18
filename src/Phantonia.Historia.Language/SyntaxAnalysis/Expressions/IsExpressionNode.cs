using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IsExpressionNode() : ExpressionNode
{
    public required string OutcomeName { get; init; }

    public required string OptionName { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"{OutcomeName} is {OptionName}";
}
