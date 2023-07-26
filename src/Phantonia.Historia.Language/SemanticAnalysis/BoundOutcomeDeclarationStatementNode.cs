﻿using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundOutcomeDeclarationStatementNode : OutcomeDeclarationStatementNode, IBoundOutcomeDeclarationNode
{
    public BoundOutcomeDeclarationStatementNode() { }

    public required OutcomeSymbol Outcome { get; init; }

    SyntaxNode IBoundOutcomeDeclarationNode.DeclarationNode => this;
}
