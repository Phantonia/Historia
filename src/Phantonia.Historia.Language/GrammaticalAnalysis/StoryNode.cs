using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public sealed record StoryNode : SyntaxNode
{
    public StoryNode() { }

    public required ImmutableArray<TopLevelNode> TopLevelNodes { get; init; }

    public override IEnumerable<SyntaxNode> Children => TopLevelNodes;
}
