using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public abstract record SyntaxNode : ISyntaxNode
{
    protected SyntaxNode() { }

    public required int Index { get; init; }

    public abstract IEnumerable<SyntaxNode> Children { get; }

    public ImmutableArray<Token> PrecedingTokens { get; init; } = [];

    public IEnumerable<SyntaxNode> FlattenHierarchie()
    {
        yield return this;

        foreach (SyntaxNode node in Children)
        {
            IEnumerable<SyntaxNode> nodeHierarchie = node.FlattenHierarchie();

            foreach (SyntaxNode hierarchieNode in nodeHierarchie)
            {
                yield return hierarchieNode;
            }
        }
    }

    public abstract string Reconstruct();

    public abstract void Reconstruct(TextWriter writer);

    protected internal abstract string GetDebuggerDisplay();
}
