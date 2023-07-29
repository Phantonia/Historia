using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record UnionTypeSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public UnionTypeSymbolDeclarationNode() { }

    public required ImmutableArray<TypeNode> Subtypes { get; init; }

    public override IEnumerable<SyntaxNode> Children => Subtypes;
}
