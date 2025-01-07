using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record EnumSymbolDeclarationNode() : TypeSymbolDeclarationNode
{
    public required Token EnumKeywordToken { get; init; }

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<Token> OptionTokens { get; init; }

    public required ImmutableArray<Token> CommaTokens { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token SemicolonToken { get; init; }

    public IEnumerable<string> Options => OptionTokens.Select(o => o.Text);

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        EnumKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        Debug.Assert(OptionTokens.Length - CommaTokens.Length is 1 or 0);

        foreach ((Token option, Token comma) in OptionTokens.Zip(CommaTokens))
        {
            option.Reconstruct(writer);
            comma.Reconstruct(writer);
        }

        if (OptionTokens.Length - CommaTokens.Length is 1)
        {
            OptionTokens[^1].Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"enum type {Name} ({string.Join(", ", Options)})";
}
