using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record BranchOnStatementNode() : StatementNode
{
    public required Token BranchOnKeywordToken { get; init; }

    public required Token OutcomeNameToken { get; init; }

    public string OutcomeName => OutcomeNameToken.Text;

    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<BranchOnOptionNode> Options { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Options;

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(BranchOnKeywordToken.Reconstruct());
        writer.Write(OutcomeNameToken.Reconstruct());
        writer.Write(OpenBraceToken.Reconstruct());

        foreach (BranchOnOptionNode option in Options)
        {
            option.Reconstruct(writer);
        }

        writer.Write(ClosedBraceToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"branchon {OutcomeName} {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";
}
