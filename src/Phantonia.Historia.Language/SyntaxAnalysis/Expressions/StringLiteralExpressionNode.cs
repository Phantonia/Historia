using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record StringLiteralExpressionNode() : ExpressionNode
{
    public required Token StringLiteralToken { get; init; }

    public string StringLiteral => StringLiteralToken.StringValue!;

    public override IEnumerable<SyntaxNode> Children => [];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(StringLiteralToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"string {StringLiteral}";
}
