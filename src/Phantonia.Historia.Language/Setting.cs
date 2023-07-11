namespace Phantonia.Historia.Language;

public readonly record struct Setting
{
    public required SettingKind Kind { get; init; }

    public required SettingName Name { get; init; }
}
