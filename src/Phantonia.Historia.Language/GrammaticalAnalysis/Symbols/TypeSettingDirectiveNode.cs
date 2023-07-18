using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public sealed record TypeSettingDirectiveNode : SettingDirectiveNode
{
    public TypeSettingDirectiveNode() { }

    public required TypeNode Type { get; init; }
}
