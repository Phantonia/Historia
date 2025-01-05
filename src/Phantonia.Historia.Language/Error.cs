namespace Phantonia.Historia.Language;

public readonly record struct Error
{
    public Error() { }

    public required string ErrorMessage { get; init; }

    public required long Index { get; init; }
}
