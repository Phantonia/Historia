using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record ExpressionSettingDirectiveNode : SettingDirectiveNode
{
    public ExpressionSettingDirectiveNode() { }

    public required ExpressionNode Expression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Expression };
}
