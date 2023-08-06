using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record ExpressionSettingDirectiveNode : SettingDirectiveNode
{
    public ExpressionSettingDirectiveNode() { }

    public required ExpressionNode Expression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Expression };
}
