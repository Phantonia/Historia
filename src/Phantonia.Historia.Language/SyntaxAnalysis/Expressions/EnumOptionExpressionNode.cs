using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record EnumOptionExpressionNode() : ExpressionNode
{
    public required string EnumName { get; init; }

    public required string OptionName { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"enum option {EnumName}.{OptionName}";
}
