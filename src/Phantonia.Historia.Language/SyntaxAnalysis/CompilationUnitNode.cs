using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record CompilationUnitNode : SyntaxNode
{
    public required string Path { get; init; }

    public required long Length { get; init; }

    public required ImmutableArray<TopLevelNode> TopLevelNodes { get; init; }

    public bool IsModified { get; init; } = false;

    public override IEnumerable<SyntaxNode> Children => TopLevelNodes;

    protected override void ReconstructCore(TextWriter writer)
    {
        foreach (TopLevelNode node in TopLevelNodes)
        {
            node.Reconstruct(writer);
        }
    }

    protected internal override string GetDebuggerDisplay() => $"compilation unit @{Path} w/ {TopLevelNodes.Length} top level nodes";
}
