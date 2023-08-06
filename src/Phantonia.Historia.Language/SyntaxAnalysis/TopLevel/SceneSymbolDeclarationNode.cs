using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Body };
}
