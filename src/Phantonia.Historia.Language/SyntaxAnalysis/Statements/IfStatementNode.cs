using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record IfStatementNode() : StatementNode, IBranchingStatementNode
{
    public required ExpressionNode Condition { get; init; }

    public required StatementBodyNode ThenBlock { get; init; }

    public required StatementBodyNode? ElseBlock { get; init; }

    public override IEnumerable<SyntaxNode> Children => ElseBlock is null ? [Condition, ThenBlock] : [Condition, ThenBlock, ElseBlock];

    protected internal override string GetDebuggerDisplay()
        => $"if ({Condition.GetDebuggerDisplay()}) run {ThenBlock.Statements.Length} statement(s), else run {ElseBlock?.Statements.Length ?? 0} statement(s)";

    IEnumerable<StatementBodyNode> IBranchingStatementNode.Bodies => ElseBlock is null ? [ThenBlock] : [ThenBlock, ElseBlock];
}
