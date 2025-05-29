using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record StringLiteralExpressionNode() : ExpressionNode
{
    public required Token LiteralToken { get; init; }

    public string StringLiteral => LiteralToken.StringValue!;

    public override bool IsConstant => true;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        LiteralToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"string {StringLiteral}";
}
