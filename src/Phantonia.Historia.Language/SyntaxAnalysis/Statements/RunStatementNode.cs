using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record RunStatementNode() : StatementNode
{
    public required string ReferenceName { get; init; }

    public required string MethodName { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;

    protected internal override string GetDebuggerDisplay() => $"run {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
