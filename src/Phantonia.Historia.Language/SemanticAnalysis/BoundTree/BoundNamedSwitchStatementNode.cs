using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundNamedSwitchStatementNode() : SwitchStatementNode, IBoundOutcomeDeclarationNode
{
    public required OutcomeSymbol Outcome { get; init; }

    SyntaxNode IBoundOutcomeDeclarationNode.DeclarationNode => this;
}
