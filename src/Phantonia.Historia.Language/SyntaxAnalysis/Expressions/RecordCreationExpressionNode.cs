using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record RecordCreationExpressionNode() : ExpressionNode
{
    public required string RecordName { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;

    protected internal override string GetDebuggerDisplay() => $"creation {RecordName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
