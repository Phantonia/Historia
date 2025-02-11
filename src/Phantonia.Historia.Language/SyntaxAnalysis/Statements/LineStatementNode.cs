using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record LineStatementNode() : StatementNode, IArgumentContainerNode
{
    public required ExpressionNode CharacterExpression { get; init; }

    public required Token? OpenSquareBracketToken { get; init; }

    public required ImmutableArray<ArgumentNode>? AdditionalArguments { get; init; }

    public required Token? ClosedSquareBracketToken { get; init; }

    public required Token ColonToken { get; init; }

    public required ExpressionNode TextExpression { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [CharacterExpression, .. AdditionalArguments ?? [], TextExpression];

    protected override void ReconstructCore(TextWriter writer)
    {
        CharacterExpression.Reconstruct(writer);
        OpenSquareBracketToken?.Reconstruct(writer);

        if (AdditionalArguments is not null)
        {
            foreach (ArgumentNode argument in (ImmutableArray<ArgumentNode>)AdditionalArguments)
            {
                argument.Reconstruct(writer);
            }
        }

        ClosedSquareBracketToken?.Reconstruct(writer);
        ColonToken.Reconstruct(writer);
        TextExpression.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() =>
        AdditionalArguments is null
        ? $"line {CharacterExpression.GetDebuggerDisplay()}: {TextExpression.GetDebuggerDisplay()}"
        : $"line {CharacterExpression.GetDebuggerDisplay()} [{string.Join(", ", AdditionalArguments?.Select(a => a.GetDebuggerDisplay())!)}";

    ImmutableArray<ArgumentNode> IArgumentContainerNode.Arguments
    {
        get => AdditionalArguments ?? [];
        init => AdditionalArguments = value;
    }
}
