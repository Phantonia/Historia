using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class SpecTests
{
    private Binder PrepareBinder(string code)
    {
        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), "");
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        CompilationUnitNode unit = parser.Parse();

        StoryNode story = new()
        {
            CompilationUnits = [unit],
            Index = 0,
            Length = unit.Length,
            PrecedingTokens = [],
        };

        Binder binder = new(story);
        return binder;
    }

    [TestMethod]
    public void Test_1_2_1_2_RecordPropertyNamesHaveToBeDifferent()
    {
        // spec 1.2.1.2 "All the property names must be different."
        string code =
            """
            record X(A: String, A: Int);

            scene main { }
            """;

        Binder binder = PrepareBinder(code);

        List<Error> errors = new();
        binder.ErrorFound += errors.Add;

        _ = binder.Bind();

        Assert.IsTrue(errors.Count > 0);
    }
}
