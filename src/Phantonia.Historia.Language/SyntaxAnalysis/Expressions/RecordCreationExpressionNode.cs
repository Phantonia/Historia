using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record RecordCreationExpressionNode : ExpressionNode
{
    public RecordCreationExpressionNode() { }

    public required string RecordName { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;
}
