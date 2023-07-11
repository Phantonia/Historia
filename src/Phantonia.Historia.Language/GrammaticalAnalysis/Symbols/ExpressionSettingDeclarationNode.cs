using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public sealed record ExpressionSettingDeclarationNode : SettingSymbolDeclarationNode
{
    public ExpressionSettingDeclarationNode() { }

    public required ExpressionNode Expression { get; init; }
}
