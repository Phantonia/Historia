﻿using System.Collections.Generic;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public abstract record SyntaxNode : ISyntaxNode
{
    protected SyntaxNode() { }

    public required long Index { get; init; }

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

    protected internal abstract string GetDebuggerDisplay();
}
