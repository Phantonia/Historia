using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public abstract record SyntaxNode
{
    protected SyntaxNode() { }

    public required int Index { get; init; }

    public abstract IEnumerable<SyntaxNode> Children { get; }

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
}
