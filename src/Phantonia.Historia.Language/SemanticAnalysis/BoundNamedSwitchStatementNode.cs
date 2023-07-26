using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundNamedSwitchStatementNode : SwitchStatementNode, IBoundOutcomeDeclarationNode
{
    public BoundNamedSwitchStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }

    SyntaxNode IBoundOutcomeDeclarationNode.DeclarationNode => this;
}
