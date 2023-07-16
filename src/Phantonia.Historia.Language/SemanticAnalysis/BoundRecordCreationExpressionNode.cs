using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundRecordCreationExpressionNode : ExpressionNode
{
    public BoundRecordCreationExpressionNode() { }

    public required RecordCreationExpressionNode CreationExpression { get; init; }

    public required ImmutableArray<BoundArgumentNode> BoundArguments { get; init; }

    public required RecordTypeSymbol Record { get; init; }
}
