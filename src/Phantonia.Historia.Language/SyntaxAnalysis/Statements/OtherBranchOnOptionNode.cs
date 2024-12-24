using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OtherBranchOnOptionNode() : BranchOnOptionNode
{
    public required Token OtherKeywordToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];

    internal override string ReconstructCore() => OptionKeywordToken.Reconstruct() + OtherKeywordToken.Reconstruct() + Body.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(OptionKeywordToken.Reconstruct());
        writer.Write(OtherKeywordToken.Reconstruct());
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"option other w/ {Body.Statements.Length} statements";
}
