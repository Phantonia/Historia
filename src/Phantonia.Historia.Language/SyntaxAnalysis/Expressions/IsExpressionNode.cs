using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record IsExpressionNode() : ExpressionNode
{
    public required Token OutcomeNameToken { get; init; }

    public string OutcomeName => OutcomeNameToken.Text;

    public required Token IsKeywordToken { get; init; }

    public required Token OptionNameToken { get; init; }

    public string OptionName => OptionNameToken.Text;

    public override IEnumerable<SyntaxNode> Children => [];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(OutcomeNameToken.Reconstruct());
        writer.Write(IsKeywordToken.Reconstruct());
        writer.Write(OptionNameToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"{OutcomeName} is {OptionName}";
}
