using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record ArgumentNode() : SyntaxNode
{
    public required Token? PropertyNameToken { get; init; }

    public string? PropertyName => PropertyNameToken?.Text;

    public required Token? EqualsToken { get; init; }

    public required ExpressionNode Expression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression];

    protected override void ReconstructCore(TextWriter writer)
    {
        PropertyNameToken?.Reconstruct(writer);
        EqualsToken?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"argument {PropertyName ?? ""}{EqualsToken?.Text ?? ""}{Expression.GetDebuggerDisplay()}";
}
