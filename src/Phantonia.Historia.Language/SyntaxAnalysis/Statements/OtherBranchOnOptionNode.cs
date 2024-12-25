using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OtherBranchOnOptionNode() : BranchOnOptionNode
{
    public required Token OtherKeywordToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];

    protected override void ReconstructCore(TextWriter writer)
    {
        OptionKeywordToken.Reconstruct(writer);
        OtherKeywordToken.Reconstruct(writer);
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"option other w/ {Body.Statements.Length} statements";
}
