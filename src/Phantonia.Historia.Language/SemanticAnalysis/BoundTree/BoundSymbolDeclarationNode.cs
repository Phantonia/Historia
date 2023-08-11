using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundSymbolDeclarationNode : SymbolDeclarationNode
{
    public BoundSymbolDeclarationNode() { }

    public required SymbolDeclarationNode Declaration { get; init; }

    public required Symbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Declaration };

    protected internal override string GetDebuggerDisplay() => $"{Declaration.GetDebuggerDisplay()} bound @ {Symbol.GetDebuggerDisplay()}";
}
