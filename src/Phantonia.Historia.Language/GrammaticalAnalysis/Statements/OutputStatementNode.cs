﻿using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record OutputStatementNode : StatementNode, IOutputStatementNode
{
    public OutputStatementNode() { }

    public required ExpressionNode OutputExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { OutputExpression };
}
