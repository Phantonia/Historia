namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SettingDirectiveNode() : TopLevelNode
{
    public required string SettingName { get; init; }
}
