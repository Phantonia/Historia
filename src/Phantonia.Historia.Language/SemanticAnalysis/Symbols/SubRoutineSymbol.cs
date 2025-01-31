namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record SubroutineSymbol() : Symbol
{
    public required SubroutineKind Kind { get; init; }

    public bool IsChapter => Kind is SubroutineKind.Chapter;

    public bool IsScene => Kind is SubroutineKind.Scene;

    protected internal override string GetDebuggerDisplay() => $"{(IsChapter ? "chapter" : "scene")} symbol {Name}";
}
