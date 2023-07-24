using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundTypeNode : TypeNode
{
    public BoundTypeNode() { }

    public required TypeNode Node { get; init; }

    public required TypeSymbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Node };
}
