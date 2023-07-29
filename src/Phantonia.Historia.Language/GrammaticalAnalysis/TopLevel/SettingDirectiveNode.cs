namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public abstract record SettingDirectiveNode : TopLevelNode
{
    public SettingDirectiveNode() { }

    public required string SettingName { get; init; }
}
