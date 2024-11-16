using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LoopSwitchOptionNode() : SyntaxNode
{
    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public required LoopSwitchOptionKind Kind { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression, Body];

    protected internal override string GetDebuggerDisplay()
        => $"{Kind switch
        {
            LoopSwitchOptionKind.None => "",
            LoopSwitchOptionKind.Final => "final ",
            LoopSwitchOptionKind.Loop => "loop ",
            _ => "<invalid kind> ",
        }}option ({Expression.GetDebuggerDisplay()}) w/ {Body.Statements.Length} statements";
}
