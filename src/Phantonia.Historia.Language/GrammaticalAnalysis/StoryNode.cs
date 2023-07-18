using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public sealed record StoryNode
{
    public StoryNode() { }

    public required ImmutableArray<TopLevelNode> TopLevelNodes { get; init; }
}
