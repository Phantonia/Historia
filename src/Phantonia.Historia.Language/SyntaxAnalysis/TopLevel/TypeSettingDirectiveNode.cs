using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record TypeSettingDirectiveNode : SettingDirectiveNode
{
    public TypeSettingDirectiveNode() { }

    public required TypeNode Type { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Type };

    protected internal override string GetDebuggerDisplay() => $"setting {SettingName}: {Type.GetDebuggerDisplay()}";
}
