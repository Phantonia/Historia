using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record EnumOptionExpressionNode : ExpressionNode
{
    public EnumOptionExpressionNode() { }

    public required string EnumName { get; init; }

    public required string OptionName { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    protected internal override string GetDebuggerDisplay() => $"enum option {EnumName}.{OptionName}";
}
