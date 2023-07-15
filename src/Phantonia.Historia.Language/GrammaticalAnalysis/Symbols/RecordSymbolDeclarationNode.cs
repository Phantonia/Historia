using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public sealed record RecordSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public RecordSymbolDeclarationNode() { }

    public required ImmutableArray<PropertyDeclarationNode> Properties { get; init; }
}
