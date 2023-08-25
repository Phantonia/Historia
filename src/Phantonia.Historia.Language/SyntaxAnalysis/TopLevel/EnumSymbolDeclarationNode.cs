using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record EnumSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public EnumSymbolDeclarationNode() { }

    public required ImmutableArray<string> Options { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    protected internal override string GetDebuggerDisplay() => $"enum type {Name} ({string.Join(", ", Options)})";
}
