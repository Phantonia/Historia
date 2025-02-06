using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record StoryNode() : SyntaxNode
{
    public required ImmutableArray<CompilationUnitNode> CompilationUnits { get; init; }

    public required long Length { get; init; }

    public override IEnumerable<SyntaxNode> Children => CompilationUnits;

    public IEnumerable<TopLevelNode> GetTopLevelNodes() => CompilationUnits.SelectMany(u => u.TopLevelNodes);

    protected override void ReconstructCore(TextWriter writer)
    {
        foreach (CompilationUnitNode node in CompilationUnits)
        {
            node.Reconstruct(writer);
            writer.WriteLine();
        }
    }

    protected internal override string GetDebuggerDisplay() => $"story w/ {CompilationUnits.Length} compilation nodes";
}
