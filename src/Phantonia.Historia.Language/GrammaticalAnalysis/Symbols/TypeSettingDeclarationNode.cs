using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public sealed record TypeSettingDeclarationNode : SettingSymbolDeclarationNode
{
    public TypeSettingDeclarationNode() { }

    public required TypeNode Type { get; init; }
}
