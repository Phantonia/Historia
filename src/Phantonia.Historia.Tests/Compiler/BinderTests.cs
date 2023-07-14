using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class BinderTests
{
    [TestMethod]
    public void TestNoMain()
    {
        string code =
            """
            option OutputType: String;
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: A story needs a main scene
                                        option OutputType: String;
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
                      scene main {}
                      """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        Binder binder = new(parser.Parse());

        int errorCount = 0;

        binder.ErrorFound += e =>
        {
            errorCount++;

            const string ErrorMessage = """
                                        Error: Duplicated symbol name 'main'
                                        scene main {}
                                        ^
                                        """;

            Assert.AreEqual(ErrorMessage, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();

        Assert.AreEqual(2, errorCount);
    }

    [TestMethod]
    public void TestTypeSettings()
    {
        ImmutableArray<Setting> settings = new[]
        {
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OptionType },
            new Setting { Kind = SettingKind.TypeArgument, Name = SettingName.OutputType },
        }.ToImmutableArray();

        string code =
            """
            setting OutputType: String;
            setting OptionType: Int;
            scene main { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex(), settings);
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());
        (StoryNode boundStory, SymbolTable symbolTable) = binder.Bind();

        Assert.AreEqual(3, boundStory.Symbols.Length);

        Assert.IsTrue(boundStory.Symbols[0] is TypeSettingDeclarationNode
        {
            Type: BoundTypeNode
            {
                Symbol: BuiltinTypeSymbol
                {
                    Type: BuiltinType.String,
                }
            }
        });

        Assert.IsTrue(boundStory.Symbols[1] is TypeSettingDeclarationNode
        {
            Type: BoundTypeNode
            {
                Symbol: BuiltinTypeSymbol
                {
                    Type: BuiltinType.Int,
                }
            }
        });

        Assert.IsTrue(symbolTable.IsDeclared("main"));
    }

    [TestMethod]
    public void TestNameClashes()
    {
        string code =
            """
            scene Abc { }
            scene Abc { }
            scene main { }
            """;

        Lexer lexer = new(code);
        Parser parser = new(lexer.Lex());
        parser.ErrorFound += e => Assert.Fail($"Error: {e.ErrorMessage}");

        Binder binder = new(parser.Parse());

        int errorCount = 0;
        binder.ErrorFound += e =>
        {
            errorCount++;

            Assert.AreEqual(
                """
                Error: Duplicated symbol name 'Abc'
                scene Abc { }
                ^
                """, Errors.GenerateFullMessage(code, e));
        };

        _ = binder.Bind();
        Assert.AreEqual(1, errorCount);
    }
}
