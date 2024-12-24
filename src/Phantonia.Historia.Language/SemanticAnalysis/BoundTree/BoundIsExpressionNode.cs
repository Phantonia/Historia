using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundIsExpressionNode() : ExpressionNode
{
    public required IsExpressionNode Original { get; init; }

    public required OutcomeSymbol Outcome { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Original];

    internal override string ReconstructCore() => Original.ReconstructCore();

    internal override void ReconstructCore(TextWriter writer)
    {
        Original.ReconstructCore(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"bound {Original.GetDebuggerDisplay()}";
}
