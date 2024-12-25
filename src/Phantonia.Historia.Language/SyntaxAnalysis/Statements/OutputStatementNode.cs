using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OutputStatementNode() : StatementNode, IOutputStatementNode
{
    public required Token? CheckpointKeywordToken { get; init; }

    public bool IsCheckpoint => CheckpointKeywordToken is not null;

    public required Token OutputKeywordToken { get; init; }

    public required ExpressionNode OutputExpression { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [OutputExpression];

    protected override void ReconstructCore(TextWriter writer)
    {
        CheckpointKeywordToken?.Reconstruct(writer);
        OutputKeywordToken.Reconstruct(writer);
        OutputExpression.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"output {OutputExpression.GetDebuggerDisplay()}";
}
