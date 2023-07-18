using Phantonia.Historia.Language.GrammaticalAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public readonly record struct BindingResult
{
    public static BindingResult Invalid => default;

    public BindingResult(StoryNode boundStory, SymbolTable symbolTable)
    {
        BoundStory = boundStory;
        SymbolTable = symbolTable;
        IsValid = true;
    }

    public bool IsValid { get; init; }

    public StoryNode? BoundStory { get; init; }

    public SymbolTable? SymbolTable { get; init; }

    public void Deconstruct(out StoryNode? boundStory, out SymbolTable? symbolTable)
    {
        boundStory = BoundStory;
        symbolTable = SymbolTable;
    }
}
