using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.Ast;
using System.Collections.Immutable;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class LexerTests
{
    [TestMethod]
    public void TestValidCode()
    {
        string code = """
                      scene main
                      {
                          output (42);
                      }
                      """;

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        int index = 0;

        Assert.AreEqual(TokenKind.SceneKeyword, tokens[index].Kind);
        Assert.AreEqual("scene", tokens[index].Text);
        Assert.AreEqual(0, tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.Identifier, tokens[index].Kind);
        Assert.AreEqual("main", tokens[index].Text);
        Assert.AreEqual(code.IndexOf("main"), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.OpenBrace, tokens[index].Kind);
        Assert.AreEqual("{", tokens[index].Text);
        Assert.AreEqual(code.IndexOf('{'), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.OutputKeyword, tokens[index].Kind);
        Assert.AreEqual("output", tokens[index].Text);
        Assert.AreEqual(code.IndexOf("output"), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.OpenParenthesis, tokens[index].Kind);
        Assert.AreEqual("(", tokens[index].Text);
        Assert.AreEqual(code.IndexOf("("), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.IntegerLiteral, tokens[index].Kind);
        Assert.AreEqual("42", tokens[index].Text);
        Assert.AreEqual(code.IndexOf("42"), tokens[index].Index);
        Assert.AreEqual(42, tokens[index].IntegerValue);

        index++;

        Assert.AreEqual(TokenKind.ClosedParenthesis, tokens[index].Kind);
        Assert.AreEqual(")", tokens[index].Text);
        Assert.AreEqual(code.IndexOf(")"), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.Semicolon, tokens[index].Kind);
        Assert.AreEqual(";", tokens[index].Text);
        Assert.AreEqual(code.IndexOf(";"), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.ClosedBrace, tokens[index].Kind);
        Assert.AreEqual("}", tokens[index].Text);
        Assert.AreEqual(code.IndexOf("}"), tokens[index].Index);

        index++;

        Assert.AreEqual(TokenKind.EndOfFile, tokens[index].Kind);
        Assert.AreEqual("", tokens[index].Text);
        Assert.AreEqual(code.Length, tokens[index].Index);

        index++;

        Assert.AreEqual(index, tokens.Length);
    }
}
