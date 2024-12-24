using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record SpectrumAdjustmentStatementNode() : StatementNode
{
    public required Token StrengthenOrWeakenKeywordToken { get; init; }

    public bool Strengthens => StrengthenOrWeakenKeywordToken.Kind is TokenKind.StrengthenKeyword;

    public bool Weakens => !Strengthens;

    public required Token SpectrumNameToken { get; init; }

    public string SpectrumName => SpectrumNameToken.Text;

    public required Token ByKeywordToken { get; init; }

    public required ExpressionNode AdjustmentAmount { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [AdjustmentAmount];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(StrengthenOrWeakenKeywordToken.Reconstruct());
        writer.Write(SpectrumNameToken.Reconstruct());
        writer.Write(ByKeywordToken.Reconstruct());
        AdjustmentAmount.Reconstruct();
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"{(Strengthens ? "strengthen" : "weaken")} {SpectrumName} by {AdjustmentAmount.GetDebuggerDisplay()}";
}
