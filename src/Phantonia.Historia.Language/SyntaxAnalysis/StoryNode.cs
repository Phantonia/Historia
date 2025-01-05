using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record StoryNode() : SyntaxNode
{
    public required ImmutableArray<TopLevelNode> TopLevelNodes { get; init; }

    public required long Length { get; init; }

    public override IEnumerable<SyntaxNode> Children => TopLevelNodes;

    protected override void ReconstructCore(TextWriter writer)
    {
        foreach (TopLevelNode node in TopLevelNodes)
        {
            node.Reconstruct(writer);
        }
    }

    protected internal override string GetDebuggerDisplay() => $"story w/ {TopLevelNodes.Length} top level nodes";
}
