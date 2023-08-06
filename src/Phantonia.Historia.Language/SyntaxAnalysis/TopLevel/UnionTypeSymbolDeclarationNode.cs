using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record UnionTypeSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public UnionTypeSymbolDeclarationNode() { }

    public required ImmutableArray<TypeNode> Subtypes { get; init; }

    public override IEnumerable<SyntaxNode> Children => Subtypes;
}
