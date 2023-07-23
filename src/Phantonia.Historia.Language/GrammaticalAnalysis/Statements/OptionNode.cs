using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record OptionNode : SyntaxNode
{
    public OptionNode() { }

    public string? Name { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }
}
