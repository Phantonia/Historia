using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class EmitterTests
{
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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        _ = DynamicCompiler.CompileToStory<string, string>(resultCode, "HistoriaStoryStateMachine");
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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine story = DynamicCompiler.CompileToStory(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> story = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        _ = DynamicCompiler.CompileToStory<int, int>(resultCode, "MyStory.Plot.StateMachineStateMachine");
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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine story = DynamicCompiler.CompileToStory(resultCode, "HistoriaStoryStateMachine");

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

    //[TestMethod]
    //public void TestStoryGraph()
    //{
    //    string code =
    //        """
    //        scene main
    //        {
    //            output 0;

    //            outcome X(A, B);

    //            switch (1)
    //            {
    //                option (2)
    //                {
    //                    output 3;
    //                    X = A;
    //                }

    //                option (4)
    //                {
    //                    output 5;
    //                    X = B;
    //                }
    //            }

    //            output 6;

    //            branchon X
    //            {
    //                option A
    //                {
    //                    output 7;
    //                }

    //                option B
    //                {
    //                    output 8;
    //                }
    //            }
    //        }
    //        """;

    //    (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

    //    Assert.IsTrue(result.IsValid);
    //    Assert.AreEqual(0, result.Errors.Length);

    //    Type storyGraphType = DynamicCompiler.CompileAndGetType(resultCode, "HistoriaStoryGraph");
    //    Assert.IsTrue(storyGraphType.IsAbstract && storyGraphType.IsSealed); // is static class

    //    MethodInfo? createMethod = storyGraphType.GetMethod("CreateStoryGraph");
    //    Assert.IsNotNull(createMethod);

    //    StoryGraph<int, int>? storyGraph = createMethod.Invoke(null, []) as StoryGraph<int, int>;
    //    Assert.IsNotNull(storyGraph);

    //    Assert.AreEqual(1, storyGraph.StartEdges.Count);
    //    Assert.AreEqual(storyGraph.StartEdges[0].ToVertex, code.IndexOf("output 0;"));

    //    IEnumerable<long> vertices = storyGraph.Vertices.Keys.Order();
    //    long[] expectedVertices =
    //    [
    //        code.IndexOf("output 0;"),
    //        code.IndexOf("switch (1)"),
    //        code.IndexOf("output 3;"),
    //        code.IndexOf("output 5;"),
    //        code.IndexOf("output 6;"),
    //        code.IndexOf("output 7;"),
    //        code.IndexOf("output 8;"),
    //    ];

    //    Assert.IsTrue(vertices.SequenceEqual(expectedVertices));

    //    void AssertOutgoingEdgesAre(long index, params long[] expectedAdjacentVertices)
    //    {
    //        Assert.IsTrue(storyGraph.Vertices[index].OutgoingEdges.Select(e => e.ToVertex).Order().SequenceEqual(expectedAdjacentVertices));
    //    }

    //    void AssertIncomingEdgesAre(long index, params long[] expectedAdjacentVertices)
    //    {
    //        Assert.IsTrue(storyGraph.Vertices[index].IncomingEdges.Select(e => e.FromVertex).Order().SequenceEqual(expectedAdjacentVertices));
    //    }

    //    AssertOutgoingEdgesAre(expectedVertices[0], expectedVertices[1]);
    //    Assert.AreEqual(1, storyGraph.Vertices[expectedVertices[0]].IncomingEdges.Count);
    //    Assert.AreEqual(-2, storyGraph.Vertices[expectedVertices[0]].IncomingEdges[0].FromVertex);

    //    AssertOutgoingEdgesAre(expectedVertices[1], expectedVertices[2], expectedVertices[3]);
    //    AssertIncomingEdgesAre(expectedVertices[1], expectedVertices[0]);

    //    AssertOutgoingEdgesAre(expectedVertices[2], expectedVertices[4]);
    //    AssertIncomingEdgesAre(expectedVertices[2], expectedVertices[1]);

    //    AssertOutgoingEdgesAre(expectedVertices[3], expectedVertices[4]);
    //    AssertIncomingEdgesAre(expectedVertices[3], expectedVertices[1]);

    //    AssertOutgoingEdgesAre(expectedVertices[4], expectedVertices[5], expectedVertices[6]);
    //    AssertIncomingEdgesAre(expectedVertices[4], expectedVertices[2], expectedVertices[3]);

    //    AssertOutgoingEdgesAre(expectedVertices[5], FlowGraph.Sink);
    //    AssertIncomingEdgesAre(expectedVertices[5], expectedVertices[4]);

    //    AssertOutgoingEdgesAre(expectedVertices[6], FlowGraph.Sink);
    //    AssertIncomingEdgesAre(expectedVertices[6], expectedVertices[4]);
    //}

    //[TestMethod]
    //public void TestMoreComplicatedStoryGraph()
    //{
    //    // regression test specifically for the edges at the end of switch (1) to the beginnings of branchon X
    //    string code =
    //        """
    //        scene main
    //        {
    //            output 0;

    //            outcome X (A, B);

    //            switch (1)
    //            {
    //                option (2)
    //                {
    //                    output 3;
    //                    X = A;
    //                }

    //                option (4)
    //                {
    //                    output 5;
    //                    X = B;
    //                }

    //                option (6)
    //                {
    //                    switch (7)
    //                    {
    //                        option (8)
    //                        {
    //                            output 9;
    //                            X = A;
    //                        }

    //                        option (10)
    //                        {
    //                            output 11;
    //                            X = B;
    //                        }
    //                    }
    //                }
    //            }

    //            branchon X
    //            {
    //                option A
    //                {
    //                    output 12;
    //                }

    //                option B
    //                {
    //                    output 13;
    //                }
    //            }

    //            output 14;
    //        }
    //        """;

    //    (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

    //    Assert.IsTrue(result.IsValid);
    //    Assert.AreEqual(0, result.Errors.Length);

    //    Type storyGraphType = DynamicCompiler.CompileAndGetType(resultCode, "HistoriaStoryGraph");
    //    Assert.IsTrue(storyGraphType.IsAbstract && storyGraphType.IsSealed); // is static class

    //    MethodInfo? createMethod = storyGraphType.GetMethod("CreateStoryGraph");
    //    Assert.IsNotNull(createMethod);

    //    StoryGraph<int, int>? storyGraph = createMethod.Invoke(null, []) as StoryGraph<int, int>;
    //    Assert.IsNotNull(storyGraph);

    //    Assert.IsFalse(storyGraph.Vertices.ContainsKey(code.IndexOf("branchon X")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 3"), code.IndexOf("output 12")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 3"), code.IndexOf("output 13")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 5"), code.IndexOf("output 12")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 5"), code.IndexOf("output 13")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 9"), code.IndexOf("output 12")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 9"), code.IndexOf("output 13")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 11"), code.IndexOf("output 12")));
    //    Assert.IsTrue(storyGraph.ContainsEdge(code.IndexOf("output 11"), code.IndexOf("output 13")));
    //}

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

            chapter main
            {
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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

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

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(stateMachine.TryContinue());

        Assert.AreEqual(12, stateMachine.Output);
    }

    [TestMethod]
    public void TestCallsInSwitch()
    {
        // regression test
        string code =
            """
            scene main
            {
                outcome X(A, B);

                switch 0
                {
                    option 1
                    {
                        call A;
                        X = A;
                    }

                    option 2
                    {
                        call B;
                        X = B;
                    }
                }

                branchon X
                {
                    option A
                    {
                        call C;
                    }

                    option B
                    {
                        call D;
                    }
                }

                if X is A
                {
                    call D;
                }
                else
                {
                    call C;
                }
            }

            scene A
            {
                output 3;
            }

            scene B
            {
                output 4;
            }

            scene C
            {
                output 5;
            }

            scene D
            {
                output 6;
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));

        Assert.AreEqual(3, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(5, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(6, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void TestCallsInLoopSwitch()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (1)
                    {
                        call A;
                    }
            
                    option (2)
                    {
                        call B;
                    }
                }
            }
            
            scene A
            {
                output 3;
            }
            
            scene B
            {
                output 4;
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsTrue(stateMachine.TryContinueWithOption(1));
        Assert.AreEqual(4, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));
        Assert.AreEqual(3, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.IsTrue(stateMachine.FinishedStory);
    }

    [TestMethod]
    public void TestEmptyScenes()
    {
        string code =
            """
            scene main
            {
                switch 0
                {
                    option 1
                    {
                        call A;
                    }

                    option 2
                    {
                        call B;
                    }
                }

                call B;
            }

            scene A { }
            scene B { }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
    }

    [TestMethod]
    public void TestNestedLoopSwitchesThroughScenes()
    {
        string code =
            """
            scene main
            {
                loop switch (0)
                {
                    option (1)
                    {
                        call A;
                    }

                    option (2)
                    {
                        output 3;
                    }
                }
            }

            scene A
            {
                loop switch (4)
                {
                    option (5)
                    {
                        output 6;
                    }

                    option (7)
                    {
                        output 8;
                    }
                }
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        Assert.IsTrue(stateMachine.TryContinue());

        // outer loop switch
        Assert.AreEqual(0, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));

        // inner loop switch
        Assert.AreEqual(4, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinueWithOption(1));

        Assert.AreEqual(8, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());

        Assert.AreEqual(4, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));

        Assert.AreEqual(6, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());

        // outer loop switch
        Assert.AreEqual(0, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinueWithOption(0));

        Assert.AreEqual(3, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());

        Assert.IsTrue(stateMachine.FinishedStory);
    }

    enum OutcomeX
    {
        Unset = 0,
        A,
        B,
    }

    [TestMethod]
    public void TestPublicOutcome()
    {
        string code =
            """
            public outcome X(A, B);

            scene main
            {
                X = A;
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        OutcomeX GetOutcome()
        {
            PropertyInfo? property = stateMachine.GetType().GetProperty("OutcomeX");
            Assert.IsNotNull(property);

            object? value = property.GetValue(stateMachine);
            Assert.IsNotNull(value);

            return (OutcomeX)value;
        }

        Assert.AreEqual(OutcomeX.Unset, GetOutcome());
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(OutcomeX.A, GetOutcome());
    }

    //[TestMethod]
    //public void TestStoryGraphWithLoopSwitches()
    //{
    //    string code =
    //        """
    //        scene main
    //        {
    //            loop switch 0
    //            {
    //                final option 5
    //                {
    //                    output 6;
    //                }

    //                option 1
    //                {
    //                    output 2;
    //                }

    //                option 3
    //                {
    //                    output 4;
    //                }
    //            }

    //            output 7;
    //        }
    //        """;

    //    (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

    //    Assert.IsTrue(result.IsValid);
    //    Assert.AreEqual(0, result.Errors.Length);

    //    Assembly assembly = DynamicCompiler.Compile(resultCode);

    //    Type? graphType = assembly.GetType("HistoriaStoryGraph");
    //    MethodInfo? createMethod = graphType?.GetMethod("CreateStoryGraph");
    //    object? graphObject = createMethod?.Invoke(null, []);
    //    StoryGraph<int, int>? graph = graphObject as StoryGraph<int, int>;
    //    Assert.IsNotNull(graph);

    //    long[] order = graph.TopologicalSort().ToArray();

    //    void AssertComesBefore(long earlier, long later)
    //    {
    //        Assert.IsTrue(Array.IndexOf(order, earlier) < Array.IndexOf(order, later));
    //    }

    //    long ls0 = code.IndexOf("loop switch 0");
    //    long o2 = code.IndexOf("output 2");
    //    long o4 = code.IndexOf("output 4");
    //    long o6 = code.IndexOf("output 6");
    //    long o7 = code.IndexOf("output 7");

    //    AssertComesBefore(ls0, o2);
    //    AssertComesBefore(ls0, o4);
    //    AssertComesBefore(ls0, o6);
    //    AssertComesBefore(ls0, o7);
    //    AssertComesBefore(o6, o7);
    //    AssertComesBefore(o2, o6);
    //    AssertComesBefore(o4, o6);
    //}

    //[TestMethod]
    //public void TestLoopSwitchOptionsInStoryGraph()
    //{
    //    string code =
    //        """
    //        scene main
    //        {
    //            loop switch 0
    //            {
    //                option 1 { output 2; }
    //                option 3 { output 4; }
    //            }
    //        }
    //        """;

    //    (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

    //    Assert.IsTrue(result.IsValid);
    //    Assert.AreEqual(0, result.Errors.Length);

    //    Assembly assembly = DynamicCompiler.Compile(resultCode);

    //    Type? graphType = assembly.GetType("HistoriaStoryGraph");
    //    MethodInfo? createMethod = graphType?.GetMethod("CreateStoryGraph");
    //    object? graphObject = createMethod?.Invoke(null, []);
    //    StoryGraph<int, int>? graph = graphObject as StoryGraph<int, int>;
    //    Assert.IsNotNull(graph);

    //    long index = code.IndexOf("loop switch 0");
    //    StoryVertex<int, int> vertex = graph.Vertices[index];

    //    Assert.AreEqual(0, vertex.Output);
    //    Assert.IsTrue(vertex.Options.SequenceEqual([1, 3]));
    //}

    [TestMethod]
    public void TestToString()
    {
        string code =
            """
            record Line(Character: String, Text: String);
            record Title(Text: String, Level: Int);
            union Output(Line, Title);

            setting OutputType: Output;

            scene main
            {
                output Title("The world", Level = 1);
                output Line("Alice", "Hello World");
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine stateMachine = DynamicCompiler.CompileToStory(resultCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.AreEqual("Title(Text = The world, Level = 1)", stateMachine.Output?.ToString());
        _ = stateMachine.TryContinue();
        Assert.AreEqual("Line(Character = Alice, Text = Hello World)", stateMachine.Output?.ToString());
    }

    [TestMethod]
    public void TestChapters()
    {
        string code =
            """
            public outcome X(A, B);
            public spectrum Y(A <= 1/2, B);
            public outcome Z(A, B);
            
            chapter main
            {
                output 0;
                
                X = A;
            
                call C;
            
                strengthen Y by 2;
            
                call D;
            }

            chapter C
            {
                output 1;
            }

            chapter D
            {
                Z = B;
                output 2;
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        object chapterC = ReflectionHelper.GetChapter(stateMachine.GetType().Assembly, "HistoriaStory", "C");
        ReflectionHelper.SetOutcome(chapterC, "X", 1);
        Assert.IsTrue(ReflectionHelper.IsReady(chapterC));
        stateMachine.Restore(chapterC);
        Assert.AreEqual(1, stateMachine.Output);
        Assert.AreEqual(1, stateMachine.GetPublicOutcome("X"));
        Assert.AreEqual(0, stateMachine.GetPublicOutcome("Z")); // Z is unset

        object chapterD = ReflectionHelper.GetChapter(stateMachine.GetType().Assembly, "HistoriaStory", "D");
        ReflectionHelper.SetOutcome(chapterD, "X", 2);
        ReflectionHelper.SetSpectrum(chapterD, "Y", 1, 3);
        Assert.IsTrue(ReflectionHelper.IsReady(chapterD));
        stateMachine.Restore(chapterD);
        Assert.AreEqual(2, stateMachine.Output);
        Assert.AreEqual(2, stateMachine.GetPublicOutcome("X"));
        Assert.AreEqual(2, stateMachine.GetPublicOutcome("Z"));
    }

    [TestMethod]
    public void TestChaptersInSwitch()
    {
        string code =
            """
            chapter main
            {
                switch 0
                {
                    option 1
                    {
                        call A;
                        output 3;
                    }

                    option 4
                    {
                        call B;
                        output 6;
                    }
                }

                output 7;
            }

            chapter A
            {
                output 2;
            }

            chapter B
            {
                output 5;
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        object chapterA = ReflectionHelper.GetChapter(stateMachine.GetType().Assembly, "HistoriaStory", "A");
        Assert.IsTrue(ReflectionHelper.IsReady(chapterA));
        stateMachine.Restore(chapterA);
        Assert.AreEqual(2, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(3, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(7, stateMachine.Output);

        object chapterB = ReflectionHelper.GetChapter(stateMachine.GetType().Assembly, "HistoriaStory", "B");
        Assert.IsTrue(ReflectionHelper.IsReady(chapterB));
        stateMachine.Restore(chapterB);
        Assert.AreEqual(5, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(6, stateMachine.Output);
        Assert.IsTrue(stateMachine.TryContinue());
        Assert.AreEqual(7, stateMachine.Output);
    }

    [TestMethod]
    public void TestEmptyBodies()
    {
        string code =
            """
            chapter main
            {
                call A;

                switch 1
                {
                    option 2 { }
                    option 3 { }
                }

                output 4;

                loop switch 5
                {
                    option 6 { }
                    loop option 7 { }
                    final option 8 { }
                }

                output 9;
            }

            chapter A { }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.AreEqual(1, stateMachine.Output);

        _ = stateMachine.TryContinueWithOption(1);
        Assert.AreEqual(4, stateMachine.Output);

        _ = stateMachine.TryContinue();
        Assert.AreEqual(5, stateMachine.Output);

        _ = stateMachine.TryContinueWithOption(1);
        Assert.AreEqual(5, stateMachine.Output);

        _ = stateMachine.TryContinueWithOption(0);
        Assert.AreEqual(5, stateMachine.Output);

        _ = stateMachine.TryContinueWithOption(0);
        Assert.AreEqual(5, stateMachine.Output);

        _ = stateMachine.TryContinueWithOption(1);
        Assert.AreEqual(9, stateMachine.Output);
    }

    [TestMethod]
    public void TestIfWithoutElseAtEndOfChapter()
    {
        string code =
            """
            public outcome X(A, B);

            chapter main
            {
                X = A;
                call A;
                output 2;
            }

            chapter A
            {
                if X is B
                {
                    output 1;
                }
            }
            """;

        (CompilationResult result, string resultCode) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        IStoryStateMachine<int, int> stateMachine = DynamicCompiler.CompileToStory<int, int>(resultCode, "HistoriaStoryStateMachine");

        _ = stateMachine.TryContinue();
        Assert.IsFalse(stateMachine.FinishedStory);
        Assert.AreEqual(2, stateMachine.Output);
    }
}
