using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.LexicalAnalysis;
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

    [TestMethod]
    public void TestKeywords()
    {
        string code = "scene output switch option setting blablabla";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(7, tokens.Length);
        Assert.AreEqual(TokenKind.SceneKeyword, tokens[0].Kind);
        Assert.AreEqual(TokenKind.OutputKeyword, tokens[1].Kind);
        Assert.AreEqual(TokenKind.SwitchKeyword, tokens[2].Kind);
        Assert.AreEqual(TokenKind.OptionKeyword, tokens[3].Kind);
        Assert.AreEqual(TokenKind.SettingKeyword, tokens[4].Kind);
        Assert.AreEqual(TokenKind.Identifier, tokens[5].Kind);
        Assert.AreEqual(TokenKind.EndOfFile, tokens[6].Kind);
    }

    [TestMethod]
    public void TestPunctuation()
    {
        string code = "{}();:";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(7, tokens.Length);
        Assert.AreEqual(TokenKind.OpenBrace, tokens[0].Kind);
        Assert.AreEqual(TokenKind.ClosedBrace, tokens[1].Kind);
        Assert.AreEqual(TokenKind.OpenParenthesis, tokens[2].Kind);
        Assert.AreEqual(TokenKind.ClosedParenthesis, tokens[3].Kind);
        Assert.AreEqual(TokenKind.Semicolon, tokens[4].Kind);
        Assert.AreEqual(TokenKind.Colon, tokens[5].Kind);
        Assert.AreEqual(TokenKind.EndOfFile, tokens[6].Kind);
    }
}
