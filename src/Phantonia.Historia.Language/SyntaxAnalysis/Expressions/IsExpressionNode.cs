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

    public override bool IsConstant => false;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        OutcomeNameToken.Reconstruct(writer);
        IsKeywordToken.Reconstruct(writer);
        OptionNameToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{OutcomeName} is {OptionName}";
}
