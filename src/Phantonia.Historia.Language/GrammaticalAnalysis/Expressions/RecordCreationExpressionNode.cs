using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

public sealed record RecordCreationExpressionNode : ExpressionNode
{
    public RecordCreationExpressionNode() { }

    public required string RecordName { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }
}
