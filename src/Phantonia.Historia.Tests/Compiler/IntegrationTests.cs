using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class IntegrationTests
{
    enum TestLines_Character
    {
        Alice,
        Bob,
    }

    [TestMethod]
    public void TestLines()
    {
        string code =
            """
            enum Character(Alice, Bob);
            line record Line(Character: Character, Text: String);
            line record EmotionalLine(Character: String, Emotion: String, Text: String);

            union Output(Line, EmotionalLine);

            setting OutputType: Output;

            chapter main
            {
                Alice: "Hello world";
                Bob: "Goodbye world";
                "Charlie" ["sad"]: "Yeah, goodbye world!";
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine stateMachine = DynamicCompiler.CompileToStory(csharpCode, "HistoriaStoryStateMachine");

        static void AssertIsLine(object outputValue, TestLines_Character character, string text)
        {
            dynamic line = ((dynamic)outputValue).AsLine();

            Assert.AreEqual((int)character, (int)line.Character);
            Assert.AreEqual(text, line.Text);
        }

        static void AssertIsEmotionalLine(object outputValue, string character, string emotion, string text)
        {
            dynamic line = ((dynamic)outputValue).AsEmotionalLine();

            Assert.AreEqual(character, line.Character);
            Assert.AreEqual(emotion, line.Emotion);
            Assert.AreEqual(text, line.Text);
        }

        _ = stateMachine.TryContinue();
        AssertIsLine(stateMachine.Output!, TestLines_Character.Alice, "Hello world");
        _ = stateMachine.TryContinue();
        AssertIsLine(stateMachine.Output!, TestLines_Character.Bob, "Goodbye world");
        _ = stateMachine.TryContinue();
        AssertIsEmotionalLine(stateMachine.Output!, "Charlie", "sad", "Yeah, goodbye world!");
        _ = stateMachine.TryContinue();
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void TestLineErrors()
    {
        string code =
            """
            line record X(A: Int, B: Int);
            line record Y(A: String, B: String);
            line record Z(A: String); // error: line record with too little properties

            scene main
            {
                4 [5, 6, 7]: 8; // error: no line record with 5 parameters
                9: 10; // error: line record ambiguous
            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.AreEqual(3, result.Errors.Length);

        List<Error> errors =
        [
            Errors.LineRecordWithTooLittleProperties("Z", 1, code.IndexOf("line record Z")),
            Errors.NoLineRecordWithPropertyCount(5, code.IndexOf("4 [5")),
            Errors.LineRecordAmbiguous(2, ["X", "Y"], code.IndexOf("9: 10")),
        ];

        Assert.IsTrue(result.Errors.Contains(errors[0]));
        Assert.IsTrue(result.Errors.Contains(errors[1]));
        Assert.IsTrue(result.Errors.Contains(errors[2]));
    }

    [TestMethod]
    public void TestDynamicSwitches()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);

                switch 1
                {
                    output 2;
                    X = A;
                    output 3;

                    option 4
                    {
                        output 5;
                    }

                    option 6
                    {
                        output 7;
                    }
                }
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.AreEqual(1, stateMachine.Output);
        Assert.AreEqual(2, stateMachine.Options.Count);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(2, stateMachine.Output);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(3, stateMachine.Output);
        Assert.IsFalse(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinueWithOption(1));
        Assert.AreEqual(7, stateMachine.Output);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsFalse(stateMachine.CanContinueWithoutOption);

        // new attempt
        stateMachine = (IStoryStateMachine<int, int>)Activator.CreateInstance(stateMachine.GetType())!;
        _ = stateMachine.TryContinue();
        Assert.AreEqual(1, stateMachine.Output);
        Assert.AreEqual(2, stateMachine.Options.Count);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(2, stateMachine.Output);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));
        Assert.AreEqual(5, stateMachine.Output);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsFalse(stateMachine.CanContinueWithoutOption);
    }

    [TestMethod]
    public void TestDynamicSwitchesInSnapshots()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);

                switch 1
                {
                    output 2;
                    X = A;
                    output 3;

                    option 4
                    {
                        output 5;
                    }

                    option 6
                    {
                        output 7;
                    }
                }
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");
        IStorySnapshot<int, int>? snapshot = stateMachine.CreateSnapshot();

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(1, snapshot.Output);
        Assert.AreEqual(2, snapshot.Options.Count);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(2, snapshot.Output);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(3, snapshot.Output);
        Assert.IsFalse(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinueWithOption(1);
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(7, snapshot.Output);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.IsFalse(snapshot.CanContinueWithoutOption);

        // new attempt
        snapshot = stateMachine.CreateSnapshot().TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(1, snapshot.Output);
        Assert.AreEqual(2, snapshot.Options.Count);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(2, snapshot.Output);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinueWithOption(0);
        Assert.IsNotNull(snapshot);
        Assert.AreEqual(5, snapshot.Output);
        Assert.IsTrue(snapshot.CanContinueWithoutOption);

        snapshot = snapshot.TryContinue();
        Assert.IsNotNull(snapshot);
        Assert.IsFalse(snapshot.CanContinueWithoutOption);
    }

    [TestMethod]
    public void TestSwitchContainingNestedStatements()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B) default A;

                switch 1
                {
                    if X is A
                    {
                        output 2;
                    }
                    else
                    {
                        output 3;
                    }

                    option 4 { }
                }
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        _ = stateMachine.TryContinue();
        Assert.AreEqual(2, stateMachine.Output);
        _ = stateMachine.TryContinueWithOption(0);
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void TestSwitchContainingSwitchError()
    {
        string code =
            """
            scene main
            {
                switch 1
                {
                    switch 2
                    {
                        option 3
                        {

                        }
                    }

                    option 4
                    {

                    }
                }
            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Length);

        Error expectedError = Errors.SwitchBodyContainsSwitchOrCall(code.IndexOf("switch 2"));
        Assert.AreEqual(expectedError, result.Errors[0]);
    }

    [TestMethod]
    public void TestSwitchBodyEndingInInvisibleStatement()
    {
        string code =
            """
            interface I(action A());
            reference R: I;

            scene main
            {
                outcome X(A, B) default B;

                switch 1
                {
                    X = A; // error
                    
                    option 2 { }
                }

                switch 3
                {
                    if X is A
                    {
                        run R.A(); // error
                    }
                    else { } // #1 error

                    option 4 { }
                }

                switch 5
                {
                    if X is A
                    {
                        output 6; // all good
                    } // #2 error: else block would be invisible

                    option 8 { }
                }
            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(4, result.Errors.Length);

        Error[] errors = result.Errors.OrderBy(e => e.Index).ToArray();

        Error[] expectedErrors =
        [
            Errors.SwitchBodyEndsInInvisibleStatement(code.IndexOf("X = A")),
            Errors.SwitchBodyEndsInInvisibleStatement(code.IndexOf("run")),
            Errors.SwitchBodyEndsInInvisibleStatement(code.IndexOf("} // #1")),
            Errors.SwitchBodyEndsInInvisibleStatement(code.IndexOf(" // #2")),
        ];

        foreach ((Error expected, Error actual) in expectedErrors.Zip(errors))
        {
            Assert.AreEqual(expected, actual);
        }
    }

    [TestMethod]
    public void RegressionTestCallAndLoopSwitch()
    {
        string code =
            """
            chapter main
            {
                call A;

                loop switch 0
                {
                    option 1
                    {
                        call A;
                    }

                    option 2
                    {

                    }
                }
            }

            scene A
            {

            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
    }

    [TestMethod]
    public void TestUncalledScene()
    {
        string code =
            """
            scene main
            {
                //call A;
            }

            scene A
            {
                call B;
            }

            scene B
            {
                output 0;
            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
    }

    [TestMethod]
    public void TestNegativeIntegers()
    {
        string code =
            """
            scene main
            {
                output -12;
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.AreEqual(-12, stateMachine.Output);
    }

    [TestMethod]
    public void TestBooleanLiterals()
    {
        string code =
            """
            setting OutputType: Boolean;

            scene main
            {
                output true;
                output false;
            }
            """;
        
        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<bool, int> stateMachine = DynamicCompiler.CompileToStory<bool, int>(csharpCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.AreEqual(true, stateMachine.Output);

        _ = stateMachine.TryContinue();
        Assert.AreEqual(false, stateMachine.Output);
    }
}
