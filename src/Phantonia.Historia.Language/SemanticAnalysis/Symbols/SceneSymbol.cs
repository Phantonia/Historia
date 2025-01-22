namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record SceneSymbol() : Symbol
{
    public required bool IsChapter { get; init; }

    protected internal override string GetDebuggerDisplay() => $"{(IsChapter ? "chapter" : "scene")} symbol {Name}";
}
