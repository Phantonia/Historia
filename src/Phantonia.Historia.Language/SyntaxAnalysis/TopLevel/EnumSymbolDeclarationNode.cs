using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record EnumSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public EnumSymbolDeclarationNode() { }

    public required ImmutableArray<string> Options { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"enum type {Name} ({string.Join(", ", Options)})";
}
