using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public record PropertyDeclarationNode : SyntaxNode
{
    public PropertyDeclarationNode() { }

    public required string Name { get; init; }

    public required TypeNode Type { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Type };
}
