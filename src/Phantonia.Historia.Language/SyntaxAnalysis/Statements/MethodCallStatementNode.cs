using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public abstract record MethodCallStatementNode() : StatementNode, IArgumentContainerNode
{
    public required string ReferenceName { get; init; }

    public required string MethodName { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;
}
