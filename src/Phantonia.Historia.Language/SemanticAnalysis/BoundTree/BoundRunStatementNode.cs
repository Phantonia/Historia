using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundRunStatementNode() : StatementNode
{
    public required RunStatementNode Original { get; init; }

    public required ReferenceSymbol Reference { get; init; }

    public required InterfaceMethodSymbol Method { get; init; }

    public required ImmutableArray<BoundArgumentNode> Arguments { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Original, .. Arguments];

    protected override void ReconstructCore(TextWriter writer)
    {
        Original.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"run {Reference.Name}.{Method.Name}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
