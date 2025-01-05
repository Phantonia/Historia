using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record InterfaceSymbolDeclarationNode() : SymbolDeclarationNode
{
    public required Token InterfaceKeywordToken { get; init; }

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<InterfaceMethodDeclarationNode> Methods { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Methods;

    protected override void ReconstructCore(TextWriter writer)
    {
        InterfaceKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        foreach (InterfaceMethodDeclarationNode method in Methods)
        {
            method.Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"interface {Name} w/ methods ({string.Join(", ", Methods.Select(m => m.GetDebuggerDisplay()))})";
}
