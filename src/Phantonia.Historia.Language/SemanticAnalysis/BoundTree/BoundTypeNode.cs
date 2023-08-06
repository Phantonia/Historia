using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundTypeNode : TypeNode
{
    public BoundTypeNode() { }

    public required TypeNode Node { get; init; }

    public required TypeSymbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Node };
}
