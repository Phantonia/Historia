using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundSymbolDeclarationNode : SymbolDeclarationNode
{
    public BoundSymbolDeclarationNode() { }

    public required SymbolDeclarationNode Declaration { get; init; }

    public required Symbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Declaration };
}
