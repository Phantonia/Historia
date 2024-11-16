using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Types;

public sealed record IdentifierTypeNode() : TypeNode
{
    public required string Identifier { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"type {Identifier}";
}
