using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IntegerLiteralExpressionNode() : ExpressionNode
{
    public required Token LiteralToken { get; init; }

    public int Value => LiteralToken.IntegerValue ?? 0;

    public override bool IsConstant => true;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        LiteralToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"integer {Value}";
}
