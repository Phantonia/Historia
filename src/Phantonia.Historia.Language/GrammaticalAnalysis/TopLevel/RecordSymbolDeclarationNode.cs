using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record RecordSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public RecordSymbolDeclarationNode() { }

    public required ImmutableArray<PropertyDeclarationNode> Properties { get; init; }

    public override IEnumerable<SyntaxNode> Children => Properties;
}
