using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record OutcomeDeclarationStatementNode() : StatementNode, IOutcomeDeclarationNode
{
    public required Token OutcomeKeywordToken { get; init; }

    public required Token NameToken { get; init; }

    public string Name => NameToken.Text;

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<Token> OptionNameTokens { get; init; }

    public ImmutableArray<string> Options => [.. OptionNameTokens.Select(o => o.Text)]; // TODO: optimize

    public required ImmutableArray<Token> CommaTokens { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token? DefaultKeywordToken { get; init; }

    public required Token? DefaultOptionToken { get; init; }

    public string? DefaultOption => DefaultOptionToken?.Text;

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        OutcomeKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        Debug.Assert(OptionNameTokens.Length - CommaTokens.Length is 1 or 0);

        foreach ((Token optionNameToken, Token commaToken) in OptionNameTokens.Zip(CommaTokens))
        {
            optionNameToken.Reconstruct(writer);
            commaToken.Reconstruct(writer);
        }

        if (OptionNameTokens.Length > CommaTokens.Length)
        {
            OptionNameTokens[^1].Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        DefaultKeywordToken?.Reconstruct(writer);
        DefaultOptionToken?.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"declare outcome {Name} ({string.Join(", ", Options)}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";

    bool IOutcomeDeclarationNode.IsPublic => false;
}
