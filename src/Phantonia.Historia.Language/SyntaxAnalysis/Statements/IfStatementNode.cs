using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record IfStatementNode() : StatementNode
{
    public required Token IfKeywordToken { get; init; }

    public required ExpressionNode Condition { get; init; }

    public required StatementBodyNode ThenBlock { get; init; }

    public required Token? ElseKeywordToken { get; init; }

    public required StatementBodyNode? ElseBlock { get; init; }

    public override IEnumerable<SyntaxNode> Children => ElseBlock is null ? [Condition, ThenBlock] : [Condition, ThenBlock, ElseBlock];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(IfKeywordToken.Reconstruct());
        Condition.Reconstruct(writer);
        ThenBlock.Reconstruct(writer);
        writer.Write(ElseKeywordToken?.Reconstruct() ?? "");
        ElseBlock?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay()
        => $"if ({Condition.GetDebuggerDisplay()}) run {ThenBlock.Statements.Length} statement(s), else run {ElseBlock?.Statements.Length ?? 0} statement(s)";
}
