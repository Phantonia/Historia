using Phantonia.Historia.Language.LexicalAnalysis;
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

    public required long Index { get; init; }

    public abstract IEnumerable<SyntaxNode> Children { get; }

    public required ImmutableList<Token> PrecedingTokens { get; init; } = [];

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
        StringWriter writer = new();

        foreach (Token precedingToken in PrecedingTokens)
        {
            precedingToken.Reconstruct(writer);
        }

        ReconstructCore(writer);

        return writer.ToString();
    }

    public void Reconstruct(TextWriter writer)
    {
        foreach (Token token in PrecedingTokens)
        {
            token.Reconstruct(writer);
        }

        ReconstructCore(writer);
    }

    protected abstract void ReconstructCore(TextWriter writer);

    protected internal abstract string GetDebuggerDisplay();
}
