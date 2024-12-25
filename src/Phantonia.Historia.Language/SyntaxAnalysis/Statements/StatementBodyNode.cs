using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record StatementBodyNode() : SyntaxNode
{
    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<StatementNode> Statements { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Statements;

    protected override void ReconstructCore(TextWriter writer)
    {
        OpenBraceToken.Reconstruct(writer);

        foreach (StatementNode statement in Statements)
        {
            statement.Reconstruct(writer);
        }

        ClosedBraceToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"body w/ {Statements.Length} statements";
}
