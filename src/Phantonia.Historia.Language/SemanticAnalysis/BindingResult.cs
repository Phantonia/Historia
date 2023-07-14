using Phantonia.Historia.Language.GrammaticalAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public readonly record struct BindingResult
{
    public BindingResult(StoryNode boundStory, SymbolTable symbolTable)
    {
        BoundStory = boundStory;
        SymbolTable = symbolTable;
    }

    public StoryNode BoundStory { get; init; }

    public SymbolTable SymbolTable { get; init; }

    public void Deconstruct(out StoryNode boundStory, out SymbolTable symbolTable)
    {
        boundStory = BoundStory;
        symbolTable = SymbolTable;
    }
}
