using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record ExpressionSettingDirectiveNode : SettingDirectiveNode
{
    public ExpressionSettingDirectiveNode() { }

    public required ExpressionNode Expression { get; init; }
}
