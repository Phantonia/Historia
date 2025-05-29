using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record TypedExpressionNode() : ExpressionNode
{
    public required ExpressionNode Original { get; init; }

    public required TypeSymbol SourceType { get; init; }

    public TypeSymbol? TargetType { get; init; }

    public override bool IsConstant => Original.IsConstant;

    public override IEnumerable<SyntaxNode> Children => [Original];

    protected override void ReconstructCore(TextWriter writer)
    {
        Original.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{Original.GetDebuggerDisplay()} w/ type {SourceType.GetDebuggerDisplay()} (target type: {TargetType?.GetDebuggerDisplay() ?? "none"})";
}
