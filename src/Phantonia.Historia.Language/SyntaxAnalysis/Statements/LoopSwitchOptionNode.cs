using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LoopSwitchOptionNode() : SyntaxNode
{
    public required Token? KindToken { get; init; }

    public LoopSwitchOptionKind Kind => (LoopSwitchOptionKind?)KindToken?.Kind ?? LoopSwitchOptionKind.None;

    public required Token OptionKeywordToken { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression, Body];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(KindToken?.Reconstruct() ?? "");
        writer.Write(OptionKeywordToken.Reconstruct());
        writer.Write(Expression.Reconstruct());
        writer.Write(Body.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay()
        => $"{Kind switch
        {
            LoopSwitchOptionKind.None => "",
            LoopSwitchOptionKind.Final => "final ",
            LoopSwitchOptionKind.Loop => "loop ",
            _ => "<invalid kind> ",
        }}option ({Expression.GetDebuggerDisplay()}) w/ {Body.Statements.Length} statements";
}
