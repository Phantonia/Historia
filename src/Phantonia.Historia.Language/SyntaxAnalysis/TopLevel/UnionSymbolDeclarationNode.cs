using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record UnionSymbolDeclarationNode() : TypeSymbolDeclarationNode
{
    public required ImmutableArray<TypeNode> Subtypes { get; init; }

    public override IEnumerable<SyntaxNode> Children => Subtypes;

    protected internal override string GetDebuggerDisplay() => $"declare union {Name} w/ subtypes {string.Join(", ", Subtypes.Select(s => s.GetDebuggerDisplay()))}";
}
