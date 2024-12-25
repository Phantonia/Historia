using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record UnionSymbolDeclarationNode() : TypeSymbolDeclarationNode
{
    public required Token UnionKeywordToken { get; init; }

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<TypeNode> Subtypes { get; init; }

    public required ImmutableArray<Token> CommaTokens { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Subtypes;

    protected override void ReconstructCore(TextWriter writer)
    {
        UnionKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        Debug.Assert(Subtypes.Length - CommaTokens.Length is 0 or 1);

        foreach ((TypeNode type, Token comma) in Subtypes.Zip(CommaTokens))
        {
            type.Reconstruct(writer);
            comma.Reconstruct(writer);
        }

        if (Subtypes.Length - CommaTokens.Length is 1)
        {
            Subtypes[^1].Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"declare union {Name} w/ subtypes {string.Join(", ", Subtypes.Select(s => s.GetDebuggerDisplay()))}";
}
