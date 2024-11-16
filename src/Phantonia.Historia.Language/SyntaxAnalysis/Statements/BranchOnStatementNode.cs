using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record BranchOnStatementNode() : StatementNode
{
    public required string OutcomeName { get; init; }

    public required ImmutableArray<BranchOnOptionNode> Options { get; init; }

    public override IEnumerable<SyntaxNode> Children => Options;

    protected internal override string GetDebuggerDisplay() => $"branchon {OutcomeName} {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";
}
