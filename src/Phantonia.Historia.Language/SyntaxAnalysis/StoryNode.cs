using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record StoryNode : SyntaxNode
{
    public StoryNode() { }

    public required ImmutableArray<TopLevelNode> TopLevelNodes { get; init; }

    public override IEnumerable<SyntaxNode> Children => TopLevelNodes;
}
