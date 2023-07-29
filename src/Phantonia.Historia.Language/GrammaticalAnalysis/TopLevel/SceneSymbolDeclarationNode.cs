using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record SceneSymbolDeclarationNode : SymbolDeclarationNode
{
    public SceneSymbolDeclarationNode() { }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Body };
}
