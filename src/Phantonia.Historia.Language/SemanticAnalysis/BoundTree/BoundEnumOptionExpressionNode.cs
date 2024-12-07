using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundEnumOptionExpressionNode() : EnumOptionExpressionNode
{
    public required EnumTypeSymbol EnumSymbol { get; init; }
}
