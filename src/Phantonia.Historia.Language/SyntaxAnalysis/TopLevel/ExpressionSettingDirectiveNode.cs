using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record ExpressionSettingDirectiveNode() : SettingDirectiveNode
{
    public required Token SettingKeywordToken { get; init; }

    public required Token ColonToken { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(SettingKeywordToken.Reconstruct());
        writer.Write(ColonToken.Reconstruct());
        Expression.Reconstruct(writer);
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"setting {SettingName}: {Expression.GetDebuggerDisplay()}";
}
