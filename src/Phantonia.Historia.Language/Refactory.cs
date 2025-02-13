using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public static class Refactory
{
    public static string RefactorString(string code, params IRefactoring[] refactorings)
    {
        List<Error> errors = [];
        using StringReader reader = new(code);

        Lexer lexer = new(reader);
        lexer.ErrorFound += errors.Add;
        ImmutableArray<Token> tokens = lexer.Lex();
        lexer.ErrorFound -= errors.Add;

        Parser parser = new(tokens, "");
        parser.ErrorFound += errors.Add;
        CompilationUnitNode unit = parser.Parse();
        parser.ErrorFound -= errors.Add;

        StoryNode story = new()
        {
            CompilationUnits = [unit],
            Index = 0,
            Length = unit.Length,
            PrecedingTokens = [],
        };

        Binder binder = new(story);
        BindingResult result = binder.Bind();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid code");
        }

        Debug.Assert(result.IsValid);
        story = result.BoundStory;

        foreach (IRefactoring refactoring in refactorings)
        {
            story = refactoring.Refactor(story);
        }

        using StringWriter writer = new();
        story.CompilationUnits[0].Reconstruct(writer);

        return writer.ToString();
    }

    public static bool RefactorFiles(string directory, IEnumerable<string> inputPaths, params IRefactoring[] refactorings)
    {
        List<Error> errors = [];

        List<CompilationUnitNode> compilationUnits = [];
        long previousLength = 0;

        Dictionary<string, ImmutableArray<long>> pathLines = [];

        foreach (string path in inputPaths)
        {
            string absolutePath = Path.Combine(directory, path);
            using StreamReader inputReader = new(absolutePath);

            Lexer lexer = new(inputReader, indexOffset: previousLength);
            lexer.ErrorFound += errors.Add;
            ImmutableArray<Token> tokens = lexer.Lex(out IEnumerable<long> lineIndices);
            lexer.ErrorFound -= errors.Add;

            pathLines[path] = [.. lineIndices];

            Parser parser = new(tokens, path);
            parser.ErrorFound += errors.Add;
            CompilationUnitNode unit = parser.Parse();
            parser.ErrorFound -= errors.Add;

            compilationUnits.Add(unit);
            previousLength += unit.Length;
        }

        StoryNode story = new()
        {
            CompilationUnits = [.. compilationUnits],
            Index = 0,
            Length = previousLength,
            PrecedingTokens = [],
        };

        Binder binder = new(story);
        binder.ErrorFound += errors.Add;
        BindingResult bindingResult = binder.Bind();
        binder.ErrorFound -= errors.Add;

        if (!bindingResult.IsValid || errors.Count > 0)
        {
            return false;
        }

        story = bindingResult.BoundStory;

        foreach (IRefactoring refactoring in refactorings)
        {
            story = refactoring.Refactor(story);
        }

        foreach (CompilationUnitNode unit in story.CompilationUnits)
        {
            using StreamWriter writer = new(Path.Combine(directory, unit.Path));
            unit.Reconstruct(writer);
        }

        return true;
    }
}
