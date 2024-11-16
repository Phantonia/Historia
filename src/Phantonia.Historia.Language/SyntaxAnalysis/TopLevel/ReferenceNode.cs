using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record ReferenceSymbolDeclarationNode : SymbolDeclarationNode
{
    public ReferenceSymbolDeclarationNode() { }

    public required string InterfaceName { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"reference {Name}: {InterfaceName}";
}
