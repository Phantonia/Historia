using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record SwitchStatementNode() : StatementNode, IOutputStatementNode
{
    public required Token? CheckpointKeywordToken { get; init; }

    public bool IsCheckpoint => CheckpointKeywordToken is not null;

    public required Token SwitchKeywordToken { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [OutputExpression, .. Options];

    protected override void ReconstructCore(TextWriter writer)
    {
        CheckpointKeywordToken?.Reconstruct(writer);
        SwitchKeywordToken.Reconstruct(writer);
        OutputExpression.Reconstruct(writer);
        OpenBraceToken.Reconstruct(writer);

        foreach (OptionNode option in Options)
        {
            option.Reconstruct(writer);
        }

        ClosedBraceToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"switch {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";
}
