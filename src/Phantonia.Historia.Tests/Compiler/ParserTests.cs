using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.Ast;
using Phantonia.Historia.Language.Ast.Expressions;
using Phantonia.Historia.Language.Ast.Statements;
using Phantonia.Historia.Language.Ast.Symbols;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class ParserTests
{
    [TestMethod]
    public void TestSimpleProgram()
    {
        string code = """
                      scene main
                      {
                          output (42);
                          output 64;
                      }
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        StoryNode story = parser.Parse();

        Assert.AreEqual(1, story.Symbols.Length);
        SceneSymbolDeclarationNode? scene = story.Symbols[0] as SceneSymbolDeclarationNode;
        Assert.IsNotNull(scene);

        Assert.AreEqual(2, scene.Body.Statements.Length);
        OutputStatementNode? outputStatement = scene.Body.Statements[0] as OutputStatementNode;
        Assert.IsNotNull(outputStatement);

        Assert.IsTrue(outputStatement is { Expression: IntegerLiteralExpressionNode { Value: 42 } });

        Assert.IsTrue(scene.Body.Statements[1] is OutputStatementNode);
    }

    [TestMethod]
    public void TestError()
    {
        string code = """
                      scene main { }
                      hello!
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());

        bool foundError = false;

        parser.ErrorFound += e =>
        {
            if (!foundError)
            {
                Assert.AreEqual("Error: Unexpected token 'hello'\r\nhello!\r\n^", Errors.GenerateFullMessage(code, e));
            }
            else
            {
                Assert.AreEqual("Error: Unexpected token '!'\r\nhello!\r\n     ^", Errors.GenerateFullMessage(code, e));
            }

            foundError = true;
        };

        _ = parser.Parse();

        Assert.IsTrue(foundError);
    }
}
