using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record InterfaceSymbolDeclarationNode : SymbolDeclarationNode
{
    public required ImmutableArray<InterfaceMethodDeclarationNode> Methods { get; init; }

    public override IEnumerable<SyntaxNode> Children => Methods;

    protected internal override string GetDebuggerDisplay() => $"interface {Name} w/ methods ({string.Join(", ", Methods.Select(m => m.GetDebuggerDisplay()))})";
}
