using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record ContinueStatementNode : StatementNode
{
    public required Token ContinueKeywordToken { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        ContinueKeywordToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"continue statement";
}
