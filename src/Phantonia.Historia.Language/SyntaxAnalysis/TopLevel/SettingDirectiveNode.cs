namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SettingDirectiveNode : TopLevelNode
{
    public SettingDirectiveNode() { }

    public required string SettingName { get; init; }
}
