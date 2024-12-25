using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record ExpressionSettingDirectiveNode() : SettingDirectiveNode
{
    public required ExpressionNode Expression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression];

    protected override IReconstructable GetValue() => Expression;

    protected internal override string GetDebuggerDisplay() => $"setting {SettingName}: {Expression.GetDebuggerDisplay()}";
}
