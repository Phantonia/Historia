using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Types;

public sealed record IdentifierTypeNode : TypeNode
{
    public IdentifierTypeNode() { }

    public required string Identifier { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"type {Identifier}";
}
