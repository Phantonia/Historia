namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public abstract record SettingSymbolDeclarationNode : SymbolDeclarationNode
{
    public SettingSymbolDeclarationNode() { }

    public required SettingName SettingName { get; init; }
}
