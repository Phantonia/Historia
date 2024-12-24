using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record InterfaceSymbolDeclarationNode() : SymbolDeclarationNode
{
    public required Token InterfaceKeywordToken { get; init; }

    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<InterfaceMethodDeclarationNode> Methods { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Methods;

    internal override void ReconstructCore(TextWriter writer)
    {
        InterfaceKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenBraceToken.Reconstruct(writer);

        foreach (InterfaceMethodDeclarationNode method in Methods)
        {
            method.Reconstruct(writer);
        }

        ClosedBraceToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"interface {Name} w/ methods ({string.Join(", ", Methods.Select(m => m.GetDebuggerDisplay()))})";
}
