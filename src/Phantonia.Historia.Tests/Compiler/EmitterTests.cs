using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class EmitterTests
{
    // debug this to generate the class
    // less than ideal i know
    // [TestMethod] // emitter not ready for this
    public void PrivateClassGenerator()
    {
        string code =
            """
            scene main
            {
                output (16);
                
                switch (17)
                {
                    option (18)
                    {
                        output (19);
                        output (20);
                    }

                    option (21)
                    {
                        output (22);

                        switch (23)
                        {
                            option (24)
                            {
                                output (25);
                            }

                            option (26)
                            { }
                        }
                    }
                }

                output(27);
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);

        string csharpText = sw.ToString();
    }

    [TestMethod]
    public void TestEmittedCode()
    {
        string code =
            """
            scene main
            {
                output (16);
                
                switch (17)
                {
                    option (18)
                    {
                        output (19);
                        output (20);
                    }

                    option (21)
                    {
                        output (22);

                        switch (23)
                        {
                            option (24)
                            {
                                output (25);
                            }

                            option (26)
                            { }
                        }
                    }
                }

                output(27);
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);
        Assert.IsTrue(story.TryContinue());

        // output (16);
        Assert.AreEqual(16, story.Output);
        Assert.AreEqual(0, story.Options.Count);

        // switch (17)
        Assert.IsFalse(story.TryContinueWithOption(0));
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(17, story.Output);
        Assert.AreEqual(2, story.Options.Count);
        Assert.AreEqual(18, story.Options[0]);
        Assert.AreEqual(21, story.Options[1]);

        // option (18), output (19);
        Assert.IsFalse(story.TryContinue());
        Assert.IsFalse(story.TryContinueWithOption(-1));
        Assert.IsFalse(story.TryContinueWithOption(2));
        Assert.IsFalse(story.TryContinueWithOption(int.MaxValue));
        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(19, story.Output);
        Assert.AreEqual(0, story.Options.Count);

        // output (20);
        for (int i = 0; i < 10; i++)
        {
            Assert.IsFalse(story.TryContinueWithOption(Random.Shared.Next()));
            Assert.IsFalse(story.TryContinueWithOption(-Random.Shared.Next()));
        }
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(20, story.Output);
        Assert.AreEqual(0, story.Options.Count);

        // output (27);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(27, story.Output);
        Assert.AreEqual(0, story.Options.Count);

        // done
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(default, story.Output);
        Assert.IsTrue(story.FinishedStory);
        Assert.AreEqual(0, story.Options.Count);

        Assert.IsFalse(story.TryContinue());
        Assert.IsFalse(story.TryContinueWithOption(0));
    }

    [TestMethod]
    public void TestOutcomeStory()
    {
        string code =
            """
            scene main
            {
                output 0;

                outcome X (A, B);

                switch (1)
                {
                    option (2)
                    {
                        output 3;
                        X = A;
                    }

                    option (4)
                    {
                        output 5;
                        X = B;
                    }

                    option (6)
                    {
                        switch (7)
                        {
                            option (8)
                            {
                                output 9;
                                X = A;
                            }

                            option (10)
                            {
                                output 11;
                                X = B;
                            }
                        }
                    }
                }

                branchon X
                {
                    option A
                    {
                        output 12;
                    }

                    option B
                    {
                        output 13;
                    }
                }

                output 14;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(0, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(1, story.Output);
        Assert.AreEqual(3, story.Options.Count);
        Assert.AreEqual(2, story.Options[0]);
        Assert.AreEqual(4, story.Options[1]);
        Assert.AreEqual(6, story.Options[2]);
        Assert.IsTrue(story.TryContinueWithOption(2));

        Assert.AreEqual(7, story.Output);
        Assert.AreEqual(2, story.Options.Count);
        Assert.AreEqual(8, story.Options[0]);
        Assert.AreEqual(10, story.Options[1]);
        Assert.IsTrue(story.TryContinueWithOption(1));

        Assert.AreEqual(11, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(13, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(14, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.IsTrue(story.FinishedStory);
        Assert.IsFalse(story.TryContinue());
    }

    [TestMethod]
    public void TestComplicatedStory()
    {
        string code =
            """
            setting OutputType: String;
            setting OptionType: String;

            scene main
            {
                outcome WorldEnding (Yes, No);

                output "Jonathan: Come on, Alice. Press the button. It's finally time to end this fucking world.";

                switch ("What do you do?")
                {
                    option ("Do it")
                    {
                        output "Alice: You are right, Jonathan. Let's do this!";
                        WorldEnding = Yes;
                    }

                    option ("Should I really?")
                    {
                        output "Alice: I'm really not sure this is the right thing to do. What about all the people?";
                        output "Jonathan: Don't pretend you care about all of them!";
                        output "Alice: Hmm...";
                        output "Jonathan: Press it now!";

                        switch ("What do you do now?")
                        {
                            option ("Press the button")
                            {
                                output "Alice: Okay, here goes nothing.";
                                WorldEnding = Yes;
                            }
            
                            option ("Destroy the button without pressing it")
                            {
                                output "Alice: No, I won't. No one will ever press this button.";
                                output "Jonathan: How dare you!";
                                WorldEnding = No;
                            }
                        }
                    }

                    option ("Destroy the button without pressing it")
                    {
                        output "Alice: I can't. And actually, I'll make it so no one will ever press it.";
                        output "Jonathan: How dare you!";
                        WorldEnding = No;
                    }
                }
                    
                output "Time stands still for a second...";

                branchon WorldEnding
                {
                    option Yes
                    {
                        output "And then the explosion wipes out all life on earth.";
                    }

                    option No
                    {
                        output "And then nothing happens.";
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        _ = DynamicCompiler.CompileToStory<string, string>(sw.ToString(), "HistoriaStoryStateMachine");
    }

    [TestMethod]
    public void TestUnion()
    {
        string code =
            """
            union X (String, Int);

            setting OutputType: X;

            scene main
            {
                output 2;
                output "String";
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string outputCode = sw.ToString();

        IStoryStateMachine story = DynamicCompiler.CompileToStory(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);
        Assert.IsTrue(story.TryContinue());

        IUnion<string?, int>? output = story.Output as IUnion<string?, int>;
        Assert.IsNotNull(output);
        Assert.AreEqual(2, output.Value1);
        Assert.IsTrue(output.AsObject() is int);
        Assert.IsNull(output.Value0);

        Assert.IsTrue(story.TryContinue());

        output = story.Output as IUnion<string?, int>;
        Assert.IsNotNull(output);
        Assert.AreEqual("String", output.Value0);
        Assert.IsTrue(output.AsObject() is string);
        Assert.AreEqual(default, output.Value1);

        Assert.AreEqual(0, output.Evaluate(s => 0, i => 1));
    }

    [TestMethod]
    public void TestEmptyStory()
    {
        string code =
            """
            scene main { }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);
        Assert.IsTrue(story.TryContinue());

        Assert.IsTrue(story.FinishedStory);
        Assert.IsFalse(story.TryContinue());
    }

    [TestMethod]
    public void TestSpectrum()
    {
        string code =
            """
            scene main
            {
                output 4;
                
                spectrum X (A < 1/3, B < 2/3, C <= 2/3, D);

                strengthen X by 2;
                weaken X by 5;
                strengthen X by 9;

                branchon X
                {
                    option D
                    {
                        output 3;
                    }
                    
                    option A
                    {
                        output 0;
                    }

                    option C
                    {
                        output 2;
                    }

                    option B
                    {
                        output 1;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(4, story.Output);

        Assert.IsTrue(story.TryContinue());

        // total = 2 + 5 + 9 = 16
        // positive = 2 + 9 = 11
        // denominator = 3
        // 11 * 3 = 33
        // 2 * 16 = 32
        // 32 < 33 => 2/3 < 11/16

        Assert.AreEqual(3, story.Output);

        Assert.IsTrue(story.TryContinue());

        Assert.IsTrue(story.FinishedStory);
        Assert.IsFalse(story.TryContinue());
    }

    [TestMethod]
    public void TestScenes()
    {
        string code =
            """
            scene main
            {
                output 0;
                call A;
                output 1;
                call B;
                output 2;
            }
            
            scene A
            {
                output 10;
            }
            
            scene B
            {
                output 20;
                call A;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(10, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(1, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(20, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(10, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(2, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.IsTrue(story.FinishedStory);
    }

    [TestMethod]
    public void TestSpectrumDefault()
    {
        string code =
            """
            spectrum X(A <= 1/2, B) default A;

            scene main
            {
                branchon X
                {
                    option A
                    {
                        output 0;
                    }

                    option B
                    {
                        output 1;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.NotStartedStory);
        Assert.IsFalse(story.FinishedStory);

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinue());
        Assert.IsTrue(story.FinishedStory);
    }

    [TestMethod]
    public void TestNamespace()
    {
        string code =
            """
            setting Namespace: "MyStory.Plot";
            setting StoryName: "StateMachine";

            scene main
            {
                output 2;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        _ = DynamicCompiler.CompileToStory<int, int>(sw.ToString(), "MyStory.Plot.StateMachineStateMachine");
    }

    private enum Character
    {
        Alice,
        Beverly,
        Charlotte,
    }

    private enum Stuff
    {
        Toaster,
        Love,
        Potsdam,
    }

    [TestMethod]
    public void TestEnum()
    {
        string code =
            """
            enum Character (Alice, Beverly, Charlotte);
            setting OutputType: Character;

            scene main
            {
                output Character.Alice;
                output Character.Beverly;
                output Character.Charlotte;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine story = DynamicCompiler.CompileToStory(sw.ToString(), "HistoriaStoryStateMachine");

        Assert.IsTrue(story.TryContinue());

        // a boxed enum value in .NET can be unboxed as a different enum type of same underlying primite type (i.e. int in this case)
        Character character = (Character)story.Output!;
        Assert.AreEqual(Character.Alice, character);

        Assert.IsTrue(story.TryContinue());

        character = (Character)story.Output!;
        Assert.AreEqual(Character.Beverly, character);

        Assert.IsTrue(story.TryContinue());

        character = (Character)story.Output!;
        Assert.AreEqual(Character.Charlotte, character);
    }

    [TestMethod]
    public void TestLoopSwitches()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (11)
                    {
                        output 11;
                    }

                    option (12)
                    {
                        output 12;
                    }

                    loop option (13)
                    {
                        output 13;
                    }

                    final option (14)
                    {
                        output 14;
                    }

                    final option (15)
                    {
                        output 15;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);
        Assert.AreEqual(5, story.Options.Count);

        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(11, story.Output);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);
        Assert.AreEqual(4, story.Options.Count);

        Assert.IsTrue(story.TryContinueWithOption(1));
        Assert.AreEqual(13, story.Output);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(12, story.Output);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(13, story.Output);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(13, story.Output);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(0, story.Output);

        Assert.IsTrue(story.TryContinueWithOption(2));
        Assert.AreEqual(15, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.IsTrue(story.FinishedStory);
    }

    [TestMethod]
    public void TestExhaustiveLoopSwitch()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (1)
                    {
                        output 1;
                    }

                    option (2)
                    {
                        output 2;
                    }

                    option (3)
                    {
                        output 3;
                    }
                }

                output 100;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(0, story.Output);
        Assert.AreEqual(3, story.Options.Count);
        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(1, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(0, story.Output);
        Assert.AreEqual(2, story.Options.Count);
        Assert.IsTrue(story.TryContinueWithOption(1));
        Assert.AreEqual(3, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(0, story.Output);
        Assert.AreEqual(1, story.Options.Count);
        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(2, story.Output);
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(100, story.Output);
    }

    [TestMethod]
    public void TestRunningLoopSwitchTwice()
    {
        string code =
            """
            scene main
            {
                call LoopSwitch;
                call LoopSwitch;
            }

            scene LoopSwitch
            {
                loop switch (0)
                {
                    option (1)
                    {
                        output 1;
                    }

                    option (2)
                    {
                        output 2;
                    }

                    final option (3)
                    {
                        output 3;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(3, story.Options.Count);
        Assert.IsTrue(story.TryContinueWithOption(0)); // option (1)
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(2, story.Options.Count);
        Assert.IsTrue(story.TryContinueWithOption(1)); // final option (3)
        Assert.IsTrue(story.TryContinue());

        Assert.AreEqual(3, story.Options.Count);
    }

    [TestMethod]
    public void TestPublicOutcomes()
    {
        string code =
            """
            public outcome X (A, B, C);
            public spectrum Y (A <= 1/2, B);
            outcome Z (A, B);

            scene main
            {
                X = A;
                strengthen Y by 7;
                weaken Y by 11;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Type storyType = story.GetType();

        MemberInfo[] outcomeX = storyType.GetMember("OutcomeX");
        Assert.AreEqual(1, outcomeX.Length);
        Assert.IsTrue(outcomeX[0] is PropertyInfo property && property.PropertyType.IsEnum);

        MemberInfo[] spectrumY = storyType.GetMember("SpectrumY");
        Assert.AreEqual(1, spectrumY.Length);
        Assert.IsTrue(spectrumY[0] is PropertyInfo anotherProperty && anotherProperty.PropertyType.IsEnum);

        MemberInfo[] valueY = storyType.GetMember("ValueY");
        Assert.AreEqual(1, valueY.Length);
        Assert.IsTrue(valueY[0] is PropertyInfo yetAnotherProperty && yetAnotherProperty.PropertyType == typeof(double));
    }

    [TestMethod]
    public void TestSnapshots()
    {
        string code =
            """
            scene main
            {
                output 0;

                outcome X(A, B);

                switch (1)
                {
                    option (2)
                    {
                        output 3;
                        X = A;
                    }

                    option (4)
                    {
                        output 5;
                        X = B;
                    }
                }

                output 6;

                branchon X
                {
                    option A
                    {
                        output 7;
                    }

                    option B
                    {
                        output 8;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");
        IStorySnapshot<int, int> snapshot = stateMachine.CreateSnapshot();

        Assert.IsTrue(snapshot.NotStartedStory);
        Assert.IsFalse(snapshot.FinishedStory);

        IStorySnapshot<int, int>? snapshot0 = snapshot.TryContinue();
        Assert.IsNotNull(snapshot0);
        Assert.AreEqual(0, snapshot0.Output);
        Assert.AreEqual(0, snapshot0.Options.Count);

        IStorySnapshot<int, int>? snapshot1 = snapshot0.TryContinue();
        Assert.IsNotNull(snapshot1);
        Assert.AreEqual(1, snapshot1.Output);
        Assert.AreEqual(2, snapshot1.Options.Count);
        Assert.AreEqual(2, snapshot1.Options[0]);
        Assert.AreEqual(4, snapshot1.Options[1]);
        Assert.IsNull(snapshot1.TryContinue());

        IStorySnapshot<int, int>? snapshot3 = snapshot1.TryContinueWithOption(0);
        Assert.IsNotNull(snapshot3);
        Assert.AreEqual(3, snapshot3.Output);
        Assert.AreEqual(0, snapshot3.Options.Count);

        IStorySnapshot<int, int>? snapshot5 = snapshot1.TryContinueWithOption(1);
        Assert.IsNotNull(snapshot5);
        Assert.AreEqual(5, snapshot5.Output);

        IStorySnapshot<int, int>? snapshot6a = snapshot3.TryContinue();
        Assert.IsNotNull(snapshot6a);
        Assert.AreEqual(6, snapshot6a.Output);

        IStorySnapshot<int, int>? snapshot6b = snapshot5.TryContinue();
        Assert.IsNotNull(snapshot6b);
        Assert.AreEqual(6, snapshot6b.Output);

        IStorySnapshot<int, int>? snapshot7 = snapshot6a.TryContinue();
        Assert.IsNotNull(snapshot7);
        Assert.AreEqual(7, snapshot7.Output);

        IStorySnapshot<int, int>? snapshot8 = snapshot6b.TryContinue();
        Assert.IsNotNull(snapshot8);
        Assert.AreEqual(7, snapshot7.Output);

        IStorySnapshot<int, int>? snapshotE = snapshot7.TryContinue();
        Assert.IsNotNull(snapshotE);
        Assert.IsTrue(snapshotE.FinishedStory);
    }

    [TestMethod]
    public void TestStoryGraph()
    {
        string code =
            """
            scene main
            {
                output 0;

                outcome X(A, B);

                switch (1)
                {
                    option (2)
                    {
                        output 3;
                        X = A;
                    }

                    option (4)
                    {
                        output 5;
                        X = B;
                    }
                }

                output 6;

                branchon X
                {
                    option A
                    {
                        output 7;
                    }

                    option B
                    {
                        output 8;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        Type storyGraphType = DynamicCompiler.CompileAndGetType(resultCode, "HistoriaStoryGraph");
        Assert.IsTrue(storyGraphType.IsAbstract && storyGraphType.IsSealed); // is static class

        MethodInfo? createMethod = storyGraphType.GetMethod("CreateStoryGraph");
        Assert.IsNotNull(createMethod);

        StoryGraph<int, int>? storyGraph = createMethod.Invoke(null, []) as StoryGraph<int, int>;
        Assert.IsNotNull(storyGraph);

        Assert.AreEqual(1, storyGraph.StartEdges.Count);
        Assert.AreEqual(storyGraph.StartEdges[0].ToVertex, code.IndexOf("output 0;"));

        IEnumerable<int> vertices = storyGraph.Vertices.Keys.Order();
        int[] expectedVertices =
        [
            code.IndexOf("output 0;"),
            code.IndexOf("switch (1)"),
            code.IndexOf("output 3;"),
            code.IndexOf("output 5;"),
            code.IndexOf("output 6;"),
            code.IndexOf("output 7;"),
            code.IndexOf("output 8;"),
        ];

        Assert.IsTrue(vertices.SequenceEqual(expectedVertices));

        void AssertOutgoingEdgesAre(int index, params int[] expectedAdjacentVertices)
        {
            Assert.IsTrue(storyGraph.Vertices[index].OutgoingEdges.Select(e => e.ToVertex).Order().SequenceEqual(expectedAdjacentVertices));
        }

        void AssertIncomingEdgesAre(int index, params int[] expectedAdjacentVertices)
        {
            Assert.IsTrue(storyGraph.Vertices[index].IncomingEdges.Select(e => e.FromVertex).Order().SequenceEqual(expectedAdjacentVertices));
        }

        AssertOutgoingEdgesAre(expectedVertices[0], expectedVertices[1]);
        Assert.AreEqual(1, storyGraph.Vertices[expectedVertices[0]].IncomingEdges.Count);
        Assert.AreEqual(-2, storyGraph.Vertices[expectedVertices[0]].IncomingEdges[0].FromVertex);

        AssertOutgoingEdgesAre(expectedVertices[1], expectedVertices[2], expectedVertices[3]);
        AssertIncomingEdgesAre(expectedVertices[1], expectedVertices[0]);

        AssertOutgoingEdgesAre(expectedVertices[2], expectedVertices[4]);
        AssertIncomingEdgesAre(expectedVertices[2], expectedVertices[1]);

        AssertOutgoingEdgesAre(expectedVertices[3], expectedVertices[4]);
        AssertIncomingEdgesAre(expectedVertices[3], expectedVertices[1]);

        AssertOutgoingEdgesAre(expectedVertices[4], expectedVertices[5], expectedVertices[6]);
        AssertIncomingEdgesAre(expectedVertices[4], expectedVertices[2], expectedVertices[3]);

        AssertOutgoingEdgesAre(expectedVertices[5], FlowGraph.FinalVertex);
        AssertIncomingEdgesAre(expectedVertices[5], expectedVertices[4]);

        AssertOutgoingEdgesAre(expectedVertices[6], FlowGraph.FinalVertex);
        AssertIncomingEdgesAre(expectedVertices[6], expectedVertices[4]);
    }

    [TestMethod]
    public void TestMoreComplicatedStoryGraph()
    {
        // regression test specifically for the edges at the end of switch (1) to the beginnings of branchon X
        string code =
            """
            scene main
            {
                output 0;

                outcome X (A, B);

                switch (1)
                {
                    option (2)
                    {
                        output 3;
                        X = A;
                    }

                    option (4)
                    {
                        output 5;
                        X = B;
                    }

                    option (6)
                    {
                        switch (7)
                        {
                            option (8)
                            {
                                output 9;
                                X = A;
                            }

                            option (10)
                            {
                                output 11;
                                X = B;
                            }
                        }
                    }
                }

                branchon X
                {
                    option A
                    {
                        output 12;
                    }

                    option B
                    {
                        output 13;
                    }
                }

                output 14;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        Type storyGraphType = DynamicCompiler.CompileAndGetType(resultCode, "HistoriaStoryGraph");
        Assert.IsTrue(storyGraphType.IsAbstract && storyGraphType.IsSealed); // is static class

        MethodInfo? createMethod = storyGraphType.GetMethod("CreateStoryGraph");
        Assert.IsNotNull(createMethod);

        StoryGraph<int, int>? storyGraph = createMethod.Invoke(null, []) as StoryGraph<int, int>;
        Assert.IsNotNull(storyGraph);

        Assert.IsFalse(storyGraph.Vertices.ContainsKey(code.IndexOf("branchon X")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 3"), code.IndexOf("output 12")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 3"), code.IndexOf("output 13")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 5"), code.IndexOf("output 12")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 5"), code.IndexOf("output 13")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 9"), code.IndexOf("output 12")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 9"), code.IndexOf("output 13")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 11"), code.IndexOf("output 12")));
        Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 11"), code.IndexOf("output 13")));
    }

    [TestMethod]
    public void TestCheckpointEmission()
    {
        string code =
            """
            public outcome X(A, B);
            public spectrum Y(A <= 1/2, B);

            scene main
            {
                X = A;

                checkpoint output 0;

                strengthen Y by 2;

                checkpoint output 1;
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        Type checkpointType = DynamicCompiler.CompileAndGetType(resultCode, "HistoriaStoryCheckpoint");

        MethodInfo? getForIndexMethod = checkpointType.GetMethod("GetForIndex");

        {
            int index = code.IndexOf("checkpoint output 0");

            object? checkpoint0 = getForIndexMethod?.Invoke(null, [index]);

            Assert.IsNotNull(checkpoint0);

            int? allegedIndex = (int?)checkpoint0.GetType().GetProperty("Index", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint0);
            Assert.AreEqual(index, allegedIndex);

            object? outcomeX = checkpointType.GetProperty("OutcomeX", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint0);
            Assert.IsNotNull(outcomeX);
            CheckpointOutcomeKind? outcomeKind = (CheckpointOutcomeKind?)outcomeX.GetType().GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance)?.GetValue(outcomeX);
            Assert.AreEqual(CheckpointOutcomeKind.Required, outcomeKind);
            object? value = outcomeX.GetType().GetProperty("Option", BindingFlags.Public | BindingFlags.Instance)?.GetValue(outcomeX);
            Assert.AreEqual(0, (int)value!); // unset

            object? spectrumY = checkpointType.GetProperty("SpectrumY", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint0);
            Assert.IsNotNull(spectrumY);
            outcomeKind = (CheckpointOutcomeKind?)spectrumY.GetType().GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.IsNotNull(outcomeKind);
            Assert.AreEqual(CheckpointOutcomeKind.NotRequired, outcomeKind);
            int? positiveCount = (int?)spectrumY.GetType().GetProperty("PositiveCount", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.AreEqual(0, positiveCount);
            int? totalCount = (int?)spectrumY.GetType().GetProperty("TotalCount", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.AreEqual(0, totalCount);
        }

        {
            int index = code.IndexOf("checkpoint output 1");

            object? checkpoint1 = getForIndexMethod?.Invoke(null, [index]);

            Assert.IsNotNull(checkpoint1);

            int? allegedIndex = (int?)checkpoint1.GetType().GetProperty("Index", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint1);
            Assert.AreEqual(index, allegedIndex);

            object? outcomeX = checkpointType.GetProperty("OutcomeX", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint1);
            Assert.IsNotNull(outcomeX);
            CheckpointOutcomeKind? outcomeKind = (CheckpointOutcomeKind?)outcomeX.GetType().GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance)?.GetValue(outcomeX);
            Assert.AreEqual(CheckpointOutcomeKind.Required, outcomeKind);
            object? value = outcomeX.GetType().GetProperty("Option", BindingFlags.Public | BindingFlags.Instance)?.GetValue(outcomeX);
            Assert.AreEqual(0, (int)value!); // unset

            object? spectrumY = checkpointType.GetProperty("SpectrumY", BindingFlags.Public | BindingFlags.Instance)?.GetValue(checkpoint1);
            Assert.IsNotNull(spectrumY);
            outcomeKind = (CheckpointOutcomeKind?)spectrumY.GetType().GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.IsNotNull(outcomeKind);
            Assert.AreEqual(CheckpointOutcomeKind.Required, outcomeKind);
            int? positiveCount = (int?)spectrumY.GetType().GetProperty("PositiveCount", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.AreEqual(0, positiveCount);
            int? totalCount = (int?)spectrumY.GetType().GetProperty("TotalCount", BindingFlags.Public | BindingFlags.Instance)?.GetValue(spectrumY);
            Assert.AreEqual(0, totalCount);
        }
    }

    [TestMethod]
    public void TestRestoreCheckpoint()
    {
        string code =
            """
            public outcome X(A, B);
            public spectrum Y(A < 2/3, B <= 2/3, C); // don't do this
            
            scene main
            {
                X = B;
                strengthen Y by 2;
            
                checkpoint output 0;
                
                branchon X
                {
                    option A
                    {
                        output 1;
                    }
                    option B
                    {
                        output 0;
                    }
                }

                branchon Y
                {
                    option A
                    {
                        output 0;
                    }
                    option B
                    {
                        output 1;
                    }
                    option C
                    {
                        output 0;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        Assembly assembly = DynamicCompiler.Compile(resultCode);

        Type? stateMachineType = assembly.GetType("HistoriaStoryStateMachine");
        Assert.IsNotNull(stateMachineType);
        IStoryStateMachine? stateMachine = (IStoryStateMachine?)Activator.CreateInstance(stateMachineType);
        MethodInfo? restoreCheckpointMethod = stateMachineType?.GetMethod("RestoreCheckpoint", BindingFlags.Public | BindingFlags.Instance);

        Type? checkpointType = assembly.GetType("HistoriaStoryCheckpoint");
        Assert.IsNotNull(checkpointType);

        // var checkpoint = HistoriaStoryCheckpoint.GetForIndex(123);
        MethodInfo? getForIndexMethod = checkpointType.GetMethod("GetForIndex");
        Assert.IsNotNull(getForIndexMethod);

        int index = code.IndexOf("checkpoint output 0;");

        object? checkpoint = getForIndexMethod.Invoke(null, [index]);
        Assert.IsNotNull(checkpoint);

        MethodInfo? isReadyMethod = checkpointType?.GetMethod("IsReady", BindingFlags.Public | BindingFlags.Instance);
        Assert.AreEqual(false, isReadyMethod?.Invoke(checkpoint, []));
        Assert.ThrowsException<TargetInvocationException>(() => restoreCheckpointMethod?.Invoke(stateMachine, [checkpoint]));

        // checkpoint.OutcomeX = checkpoint.OutcomeX.Assign(OutcomeX.A);
        PropertyInfo? outcomeXProperty = checkpoint.GetType().GetProperty("OutcomeX", BindingFlags.Public | BindingFlags.Instance);
        object? outcomeX = outcomeXProperty?.GetValue(checkpoint);
        object? optionA = assembly.GetType("OutcomeX")?.GetField("A")?.GetValue(null);
        object? assignedOutcomeX = outcomeX?.GetType().GetMethod("Assign", BindingFlags.Public | BindingFlags.Instance)?.Invoke(outcomeX, [optionA]);
        outcomeXProperty?.SetValue(checkpoint, assignedOutcomeX);

        Assert.AreEqual(false, isReadyMethod?.Invoke(checkpoint, []));
        Assert.ThrowsException<TargetInvocationException>(() => restoreCheckpointMethod?.Invoke(stateMachine, [checkpoint]));

        // checkpoint.SpectrumY = checkpoint.SpectrumY.Assign(2, 3);
        PropertyInfo? spectrumYProperty = checkpoint.GetType().GetProperty("SpectrumY", BindingFlags.Public | BindingFlags.Instance);
        CheckpointSpectrum spectrumY = (CheckpointSpectrum)spectrumYProperty?.GetValue(checkpoint)!;
        CheckpointSpectrum assignedSpectrumY = spectrumY.Assign(2, 3);
        spectrumYProperty?.SetValue(checkpoint, assignedSpectrumY);

        Assert.AreEqual(true, isReadyMethod?.Invoke(checkpoint, []));

        // stateMachine.RestoreCheckpoint(checkpoint);
        restoreCheckpointMethod?.Invoke(stateMachine, [checkpoint]);

        // test that the outcome and spectrum are good
        for (int i = 0; i < 2; i++)
        {
            Assert.IsTrue(stateMachine?.TryContinue());
            Assert.AreEqual(1, stateMachine?.Output);
        }
    }

    [TestMethod]
    public void TestReferencesAndInterfaces()
    {
        string code =
            """
            setting OptionType: String;

            interface Something
            (
                action Do(x: String, y: Int),
                choice What(x: String),
            );

            reference Domething: Something;

            scene main
            {
                checkpoint output 0;
                
                run Domething.Do("xyz", 4);

                choose Domething.What("the fuck")
                {
                    option ("YES!!!")
                    {
                        output 1;
                    }

                    option ("no :(")
                    {
                        outcome X(A, B);
                        X = A;
                        output 2;
                    }
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        Assembly assembly = DynamicCompiler.Compile(resultCode);

        // TODO: I need to proberly test this once I have build time code generation
    }

    [TestMethod]
    public void TestIfStatement()
    {
        string code =
            """
            outcome X(A, B);
            spectrum Y(A <= 1/4, B < 1/2, C <= 3/4, D);

            scene main
            {
                X = B;
                strengthen Y by 1;
                weaken Y by 1;

                if X is B and Y is C
                {
                    output 12;
                }
                else
                {
                    output 2;
                }
            }
            """;

        StringWriter sw = new();
        CompilationResult result = new Language.Compiler(code, sw).Compile();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        string resultCode = sw.ToString();

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(stateMachine.TryContinue());

        Assert.AreEqual(12, stateMachine.Output);
    }
}
