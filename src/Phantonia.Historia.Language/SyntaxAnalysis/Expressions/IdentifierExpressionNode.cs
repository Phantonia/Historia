using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IdentifierExpressionNode() : ExpressionNode
{
    public required Token IdentifierToken { get; init; }

    public string Identifier => IdentifierToken.Text;

    public override IEnumerable<SyntaxNode> Children => [];

    public override bool IsConstant => false;

    protected override void ReconstructCore(TextWriter writer)
    {
        IdentifierToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"identifier {Identifier}";
}
