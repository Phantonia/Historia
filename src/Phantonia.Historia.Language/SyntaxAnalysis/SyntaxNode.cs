﻿using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

    public string Reconstruct()
    {
        return string.Concat(PrecedingTokens.Select(t => t.Reconstruct())) + ReconstructCore();
    }

    public void Reconstruct(TextWriter writer)
    {
        foreach (Token token in PrecedingTokens)
        {
            writer.Write(token.Reconstruct());
        }

        ReconstructCore(writer);
    }

    internal abstract string ReconstructCore();

    internal abstract void ReconstructCore(TextWriter writer);

    protected internal abstract string GetDebuggerDisplay();
}
