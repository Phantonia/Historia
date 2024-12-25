using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundSymbolDeclarationNode : SymbolDeclarationNode
{
    public BoundSymbolDeclarationNode() { }

    public required SymbolDeclarationNode Original { get; init; }

    public required Symbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Original];

    protected override void ReconstructCore(TextWriter writer)
    {
        Original.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{Original.GetDebuggerDisplay()} bound @ {Symbol.GetDebuggerDisplay()}";
}
