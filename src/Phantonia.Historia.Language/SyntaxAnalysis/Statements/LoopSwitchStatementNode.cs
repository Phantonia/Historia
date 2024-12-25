using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LoopSwitchStatementNode() : StatementNode, IOutputStatementNode
{
    public required Token? CheckpointKeywordToken { get; init; }

    public bool IsCheckpoint => CheckpointKeywordToken is not null;

    public required Token LoopKeywordToken { get; init; }

    public required Token SwitchKeywordToken { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<LoopSwitchOptionNode> Options { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [OutputExpression, .. Options];

    protected override void ReconstructCore(TextWriter writer)
    {
        CheckpointKeywordToken?.Reconstruct(writer);
        LoopKeywordToken.Reconstruct(writer);
        SwitchKeywordToken.Reconstruct(writer);
        OutputExpression.Reconstruct(writer);
        OpenBraceToken.Reconstruct(writer);

        foreach (LoopSwitchOptionNode option in Options)
        {
            option.Reconstruct(writer);
        }

        ClosedBraceToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"loopswitch {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";

}
