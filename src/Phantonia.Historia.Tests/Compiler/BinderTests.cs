using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.LexicalAnalysis;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class BinderTests
{
    [TestMethod]
    public void TestNoMain()
    {
        string code = "";

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: A story needs exactly one main scene (has 0)
            
                                        ^
                                        """;

            Assert.AreEqual(ErrorMessage, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();

        Assert.AreEqual(1, errorCount);
    }

    [TestMethod]
    public void TestTooManyMains()
    {
        // so that we can distinguish the main declarations
        string code = """
                      scene main{}
                      scene main {}
                      scene main { }
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: A story needs exactly one main scene (has 3)
                                        scene main {}
                                        ^
                                        """;

            Assert.AreEqual(ErrorMessage, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();

        Assert.AreEqual(1, errorCount);
    }
}
