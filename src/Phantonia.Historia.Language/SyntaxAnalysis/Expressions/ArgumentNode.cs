using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record ArgumentNode() : SyntaxNode
{
    public required Token? ParameterNameToken { get; init; }

    public string? ParameterName => ParameterNameToken?.Text;

    public required Token? EqualsToken { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required Token? CommaToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression];

    protected override void ReconstructCore(TextWriter writer)
    {
        ParameterNameToken?.Reconstruct(writer);
        EqualsToken?.Reconstruct(writer);
        Expression.Reconstruct(writer);
        CommaToken?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"argument {ParameterName ?? ""}{EqualsToken?.Text ?? ""}{Expression.GetDebuggerDisplay()}";
}
