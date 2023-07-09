using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public sealed record StoryNode
{
    public StoryNode() { }

    public required ImmutableArray<SymbolDeclarationNode> Symbols { get; init; }
}
