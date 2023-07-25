using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record TypeSettingDirectiveNode : SettingDirectiveNode
{
    public TypeSettingDirectiveNode() { }

    public required TypeNode Type { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Type };
}
