using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record BooleanLiteralExpressionNode : ExpressionNode
{
    public required Token TrueOrFalseKeywordToken { get; init; }

    public bool Value => TrueOrFalseKeywordToken.Kind is TokenKind.TrueKeyword;

    public override bool IsConstant => true;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer) => TrueOrFalseKeywordToken.Reconstruct(writer);

    protected internal override string GetDebuggerDisplay() => $"boolean literal {TrueOrFalseKeywordToken.Text}";
}
