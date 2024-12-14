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

    public override string ReconstructCore()
    {
        StringWriter writer = new();
        ReconstructCore(writer);
        return writer.ToString();
    }

    public override void ReconstructCore(TextWriter writer)
    {
        writer.Write(EnumKeywordToken.Reconstruct());
        writer.Write(OpenParenthesisToken.Reconstruct());

        Debug.Assert(OptionTokens.Length - CommaTokens.Length is 1 or 0);

        foreach ((Token option, Token comma) in OptionTokens.Zip(CommaTokens))
        {
            writer.Write(option.Reconstruct());
            writer.Write(comma.Reconstruct());
        }

        if (OptionTokens.Length - CommaTokens.Length is 1)
        {
            writer.Write(OptionTokens[^1].Reconstruct());
        }

        writer.Write(ClosedParenthesisToken.Reconstruct());
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"enum type {Name} ({string.Join(", ", Options)})";
}
