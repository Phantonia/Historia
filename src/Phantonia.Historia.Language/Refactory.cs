using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        foreach (IRefactoring refactoring in refactorings)
        {
            story = refactoring.Refactor(story);
        }

        using StringWriter writer = new();
        story.CompilationUnits[0].Reconstruct(writer);

        return writer.ToString();
    }
}
