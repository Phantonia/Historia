﻿using Phantonia.Historia.Language.SyntaxAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public readonly record struct BindingResult
{
    public static BindingResult Invalid => default;

    public BindingResult(StoryNode boundStory, Settings settings, SymbolTable symbolTable)
    {
        BoundStory = boundStory;
        Settings = settings;
        SymbolTable = symbolTable;
        IsValid = true;
    }

    public bool IsValid { get; init; }

    public StoryNode? BoundStory { get; init; }

    public Settings? Settings { get; init; }

    public SymbolTable? SymbolTable { get; init; }

    public void Deconstruct(out StoryNode? boundStory, out Settings? settings, out SymbolTable? symbolTable)
    {
        boundStory = BoundStory;
        settings = Settings;
        symbolTable = SymbolTable;
    }
}
