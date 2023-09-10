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
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());
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

    [TestMethod]
    public void MyTestMethod()
    {
        string code =
            """
            outcome X();
            scene main { }
            """;

        StringWriter outputWriter = new();
        Language.Compiler comp = new(code, outputWriter);
        CompilationResult result = comp.Compile();

        { }
    }
}
