using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System;
using System.Collections.Generic;

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
            Type outputType = outputValue.GetType();
            Assert.AreEqual("Output", outputType.FullName);

            object? lineValue = outputType.GetProperty("Line")?.GetValue(outputValue);
            Type? lineType = lineValue?.GetType();
            Assert.AreEqual("Line", lineType?.FullName);

            object? actualCharacter = lineType?.GetProperty("Character")?.GetValue(lineValue);
            Assert.AreEqual(character, (TestLines_Character)(actualCharacter ?? 42));

            object? actualText = lineType?.GetProperty("Text")?.GetValue(lineValue);
            Assert.AreEqual(text, actualText);
        }

        static void AssertIsEmotionalLine(object outputValue, string character, string emotion, string text)
        {
            Type outputType = outputValue.GetType();
            Assert.AreEqual("Output", outputType.FullName);

            object? lineValue = outputType.GetProperty("EmotionalLine")?.GetValue(outputValue);
            Type? lineType = lineValue?.GetType();
            Assert.AreEqual("EmotionalLine", lineType?.FullName);

            object? actualCharacter = lineType?.GetProperty("Character")?.GetValue(lineValue);
            Assert.AreEqual(character, actualCharacter);

            object? actualEmotion = lineType?.GetProperty("Emotion")?.GetValue(lineValue);
            Assert.AreEqual(emotion, actualEmotion);

            object? actualText = lineType?.GetProperty("Text")?.GetValue(lineValue);
            Assert.AreEqual(text, actualText);
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
}
