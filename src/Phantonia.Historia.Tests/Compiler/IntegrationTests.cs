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

    [TestMethod]
    public void TestNonConstantExpressions()
    {
        string code =
            """
            record R(X: Boolean);

            setting OutputType: Boolean;
            setting OptionType: R;

            scene main
            {
                outcome X(A, B) default A;

                output X is A; // error
                output not X is B; // error
                output (X is B); // error
                output not (X is A); // error

                switch true // ok
                {
                    option R(X is A) { } // error
                    option R(false) { } // ok
                }
            }
            """;

        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(5, result.Errors.Length);

        Error[] errors = result.Errors.OrderBy(e => e.Index).ToArray();

        Error[] expectedErrors =
        [
            Errors.ExpectedConstantExpression(code.IndexOf("X is A")),
            Errors.ExpectedConstantExpression(code.IndexOf("not X is B")),
            Errors.ExpectedConstantExpression(code.IndexOf("(X is B)")),
            Errors.ExpectedConstantExpression(code.IndexOf("not (X is A)")),
            Errors.ExpectedConstantExpression(code.IndexOf("R(X is A)")),
        ];

        foreach ((Error expected, Error actual) in expectedErrors.Zip(errors))
        {
            Assert.AreEqual(expected, actual);
        }
    }

    [TestMethod]
    public void RegressionTestAndOr()
    {
        string code =
            """
            scene main
            {
                outcome X(A, B);
                outcome Y(A, B);
                outcome Z(A, B);

                X = A;
                Y = A;
                Z = B;

                if X is A and Y is A
                {
                    output 19;
                }

                if X is B or Z is B
                {
                    output 3;
                }

                if X is B and Z is B
                {
                    output 12;
                }
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.IsFalse(stateMachine.FinishedStory);
        Assert.AreEqual(19, stateMachine.Output);
        _ = stateMachine.TryContinue();
        Assert.IsFalse(stateMachine.FinishedStory);
        Assert.AreEqual(3, stateMachine.Output);
        _ = stateMachine.TryContinue();
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void RegressionTestSnapshotsAndFinishedStory()
    {
        string code =
            """
            scene main
            {
                output 7;
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");

        IStorySnapshot<int, int> snapshotA = stateMachine.CreateSnapshot();
        Assert.IsTrue(snapshotA.NotStartedStory);
        Assert.IsTrue(snapshotA.CanContinueWithoutOption);
        Assert.IsFalse(snapshotA.FinishedStory);

        _ = stateMachine.TryContinue();

        IStorySnapshot<int, int> snapshotB = stateMachine.CreateSnapshot();
        Assert.IsFalse(snapshotB.NotStartedStory);
        Assert.IsTrue(snapshotB.CanContinueWithoutOption);
        Assert.IsFalse(snapshotB.FinishedStory);

        _ = stateMachine.TryContinue();

        IStorySnapshot<int, int> snapshotC = stateMachine.CreateSnapshot();
        Assert.IsFalse(snapshotC.NotStartedStory);
        Assert.IsFalse(snapshotC.CanContinueWithoutOption);
        Assert.IsTrue(snapshotC.FinishedStory);

        stateMachine.GetType().GetMethod("RestoreSnapshot")?.Invoke(stateMachine, [snapshotA]);
        Assert.IsTrue(stateMachine.NotStartedStory);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsFalse(stateMachine.FinishedStory);

        stateMachine.GetType().GetMethod("RestoreSnapshot")?.Invoke(stateMachine, [snapshotB]);
        Assert.IsFalse(stateMachine.NotStartedStory);
        Assert.IsTrue(stateMachine.CanContinueWithoutOption);
        Assert.IsFalse(stateMachine.FinishedStory);

        stateMachine.GetType().GetMethod("RestoreSnapshot")?.Invoke(stateMachine, [snapshotC]);
        Assert.IsFalse(stateMachine.NotStartedStory);
        Assert.IsFalse(stateMachine.CanContinueWithoutOption);
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void TestSaveData()
    {
        string code =
            """
            outcome X(A, B, C);
            outcome Y(A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, A15, A16, A17, A18, A19, A20, A21, A22, A23, A24, A25, A26, A27, A28, A29, A30, A31, A32, A33, A34, A35, A36, A37, A38, A39, A40, A41, A42, A43, A44, A45, A46, A47, A48, A49, A50, A51, A52, A53, A54, A55, A56, A57, A58, A59, A60, A61, A62, A63, A64, A65, A66, A67, A68, A69, A70, A71, A72, A73, A74, A75, A76, A77, A78, A79, A80, A81, A82, A83, A84, A85, A86, A87, A88, A89, A90, A91, A92, A93, A94, A95, A96, A97, A98, A99, A100, A101, A102, A103, A104, A105, A106, A107, A108, A109, A110, A111, A112, A113, A114, A115, A116, A117, A118, A119, A120, A121, A122, A123, A124, A125, A126, A127, A128, A129, A130, A131, A132, A133, A134, A135, A136, A137, A138, A139, A140, A141, A142, A143, A144, A145, A146, A147, A148, A149, A150, A151, A152, A153, A154, A155, A156, A157, A158, A159, A160, A161, A162, A163, A164, A165, A166, A167, A168, A169, A170, A171, A172, A173, A174, A175, A176, A177, A178, A179, A180, A181, A182, A183, A184, A185, A186, A187, A188, A189, A190, A191, A192, A193, A194, A195, A196, A197, A198, A199, A200, A201, A202, A203, A204, A205, A206, A207, A208, A209, A210, A211, A212, A213, A214, A215, A216, A217, A218, A219, A220, A221, A222, A223, A224, A225, A226, A227, A228, A229, A230, A231, A232, A233, A234, A235, A236, A237, A238, A239, A240, A241, A242, A243, A244, A245, A246, A247, A248, A249, A250, A251, A252, A253, A254, A255, A256);

            spectrum Z(A <= 1/2, B <= 2/3, C);

            chapter main
            {
                output 19;
                X = B;
                strengthen Z by 4;
                weaken Z by 14;

                call S;

                output 8;
                call S;

                loop switch 0
                {
                    option 1
                    {

                    }

                    option 2
                    {

                    }
                }
            }

            scene S
            {
                output 20;
            }
            """;

        (CompilationResult result, string csharpCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        dynamic stateMachine = DynamicCompiler.CompileToStory<int, int>(csharpCode, "HistoriaStoryStateMachine");
        _ = stateMachine.TryContinue();
        _ = stateMachine.TryContinue();

        byte[] saveData = stateMachine.GetSaveData();

        Assert.AreEqual(0x01, saveData[0]);

        Assert.AreEqual(result.Fingerprint & 0xFFu, saveData[1]);
        Assert.AreEqual((result.Fingerprint & 0xFF00u) >> 8, saveData[2]);
        Assert.AreEqual((result.Fingerprint & 0xFF0000u) >> 16, saveData[3]);
        Assert.AreEqual((result.Fingerprint & 0xFF000000u) >> 24, saveData[4]);
        Assert.AreEqual((result.Fingerprint & 0xFF00000000u) >> 32, saveData[5]);
        Assert.AreEqual((result.Fingerprint & 0xFF0000000000u) >> 40, saveData[6]);
        Assert.AreEqual((result.Fingerprint & 0xFF000000000000u) >> 48, saveData[7]);
        Assert.AreEqual((result.Fingerprint & 0xFF00000000000000u) >> 56, saveData[8]);

        Assert.AreEqual(14, saveData[9]); // I cheated and looked at what that vertex would be, and it looks correct
        Assert.AreEqual(0, saveData[10]);
        Assert.AreEqual(0, saveData[11]);
        Assert.AreEqual(0, saveData[12]);

        // 1 byte for X which has option 1
        Assert.AreEqual(1, saveData[13]);

        // 2 bytes for Y which is unset, so uint.MaxValue
        Assert.AreEqual(0xFF, saveData[14]);
        Assert.AreEqual(0xFF, saveData[15]);

        // 8 bytes for Z which is 4/18
        // positive first, then total
        Assert.AreEqual(4, saveData[16]);
        Assert.AreEqual(0, saveData[17]);
        Assert.AreEqual(0, saveData[18]);
        Assert.AreEqual(0, saveData[19]);
        Assert.AreEqual(18, saveData[20]);
        Assert.AreEqual(0, saveData[21]);
        Assert.AreEqual(0, saveData[22]);
        Assert.AreEqual(0, saveData[23]);

        // 1 byte for S's tracker
        Assert.AreEqual(0, saveData[24]);

        // 8 bytes for loop switch which is all 0
        Assert.AreEqual(0, saveData[25]);
        Assert.AreEqual(0, saveData[26]);
        Assert.AreEqual(0, saveData[27]);
        Assert.AreEqual(0, saveData[28]);
        Assert.AreEqual(0, saveData[29]);
        Assert.AreEqual(0, saveData[30]);
        Assert.AreEqual(0, saveData[31]);
        Assert.AreEqual(0, saveData[32]);

        // 1 byte for checksum
        Assert.AreEqual(saveData[..^2].Sum(x => x) % 0x100, saveData[^1]);
    }
}
