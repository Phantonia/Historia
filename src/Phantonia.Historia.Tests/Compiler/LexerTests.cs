﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Immutable;
using System.Linq;

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
        string code = "scene output switch option setting record blablabla";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(8, tokens.Length);
        Assert.AreEqual(TokenKind.SceneKeyword, tokens[0].Kind);
        Assert.AreEqual(TokenKind.OutputKeyword, tokens[1].Kind);
        Assert.AreEqual(TokenKind.SwitchKeyword, tokens[2].Kind);
        Assert.AreEqual(TokenKind.OptionKeyword, tokens[3].Kind);
        Assert.AreEqual(TokenKind.SettingKeyword, tokens[4].Kind);
        Assert.AreEqual(TokenKind.RecordKeyword, tokens[5].Kind);
        Assert.AreEqual(TokenKind.Identifier, tokens[6].Kind);
        Assert.AreEqual(TokenKind.EndOfFile, tokens[7].Kind);
    }

    [TestMethod]
    public void TestPunctuation()
    {
        string code = "{}();:,=";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(9, tokens.Length);
        Assert.AreEqual(TokenKind.OpenBrace, tokens[0].Kind);
        Assert.AreEqual(TokenKind.ClosedBrace, tokens[1].Kind);
        Assert.AreEqual(TokenKind.OpenParenthesis, tokens[2].Kind);
        Assert.AreEqual(TokenKind.ClosedParenthesis, tokens[3].Kind);
        Assert.AreEqual(TokenKind.Semicolon, tokens[4].Kind);
        Assert.AreEqual(TokenKind.Colon, tokens[5].Kind);
        Assert.AreEqual(TokenKind.Comma, tokens[6].Kind);
        Assert.AreEqual(TokenKind.Equals, tokens[7].Kind);
        Assert.AreEqual(TokenKind.EndOfFile, tokens[8].Kind);
    }

    [TestMethod]
    public void TestStringLiterals()
    {
        string code =
            """"""
            "a" 'b' "cde" 'fghijk' """rst""" '''uvw''' """"xyz""""
            """""";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(8, tokens.Length);
        string[] literals = ["a", "b", "cde", "fghijk", "rst", "uvw", "xyz"];

        for (int i = 0; i < 7; i++)
        {
            Token t = tokens[i];
            Assert.AreEqual(TokenKind.StringLiteral, t.Kind);
            Assert.AreEqual(literals[i], tokens[i].StringValue);
        }
    }

    [TestMethod]
    public void TestBrokenStringLiterals()
    {
        string code =
            """
            "a
            "b
            """;

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(3, tokens.Length);

        Assert.AreEqual(TokenKind.BrokenStringLiteral, tokens[0].Kind);
        Assert.AreEqual(TokenKind.BrokenStringLiteral, tokens[1].Kind);
        Assert.AreEqual(TokenKind.EndOfFile, tokens[2].Kind);

        Assert.AreEqual("", tokens[0].Text);
        Assert.AreEqual("", tokens[1].Text);
    }

    [TestMethod]
    public void TestStringDelimiterMess()
    {
        string code =
            """"
            """That is "nonsense" tbh"""
            '''Hey, I'm cool'''
            """";

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(3, tokens.Length);

        Assert.AreEqual(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.AreEqual("That is \"nonsense\" tbh", tokens[0].StringValue);

        Assert.AreEqual(TokenKind.StringLiteral, tokens[1].Kind);
        Assert.AreEqual("Hey, I'm cool", tokens[1].StringValue);
    }

    [TestMethod]
    public void TestEscapeSequences()
    {
        string code =
            """
            "A\tB\uDEAD\nC"
            """;

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        Assert.AreEqual(2, tokens.Length);

        Assert.AreEqual(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.AreEqual("A\tB\uDEAD\nC", tokens[0].StringValue);
    }

    [TestMethod]
    public void TestComments()
    {
        string code =
            """
            // a very cool story
            scene main // this code will run when the story starts
            {
                output 2; // output the value 2
            } // end of story
            """;

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        TokenKind[] expectedKinds =
        [
            TokenKind.SceneKeyword,
            TokenKind.Identifier,
            TokenKind.OpenBrace,
            TokenKind.OutputKeyword,
            TokenKind.IntegerLiteral,
            TokenKind.Semicolon,
            TokenKind.ClosedBrace,
            TokenKind.EndOfFile,
        ];

        Assert.IsTrue(tokens.Select(t => t.Kind).SequenceEqual(expectedKinds));
    }

    [TestMethod]
    public void TestTrivia()
    {
        string code =
            """
            // a setting
            setting OutputType: String;

            scene main // this is the main scene
            {
                output "Some String";
                output '''S'more string''';
            }

            // ending
            """;

        Lexer lexer = new(code);
        ImmutableArray<Token> tokens = lexer.Lex();

        string reconstructedCode = string.Concat(tokens.Select(t => t.Reconstruct()));
        Assert.AreEqual(code, reconstructedCode);
    }
}
