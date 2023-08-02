using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System;
using System.Diagnostics;

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

        Language.Compiler comp = new(code);
        CompilationResult result = comp.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(result.CSharpText);

        string csharpText = result.CSharpText;
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

        CompilationResult result = new Language.Compiler(code).CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        IStory<int, int> story = DynamicCompiler.CompileToStory<int, int>(result.CSharpText);

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

        Language.Compiler compiler = new(code);
        CompilationResult result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        IStory<int, int> story = DynamicCompiler.CompileToStory<int, int>(result.CSharpText);

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

        Language.Compiler compiler = new(code);

        CompilationResult result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        _ = DynamicCompiler.CompileToStory<string, string>(result.CSharpText);
    }

    [TestMethod]
    public void TestUnion()
    {
        string code =
            """
            record Line
            {
                Text: String;
                Character: Int;
            }

            union X: String, Int, Line;

            setting OutputType: X;

            scene main
            {
                output 2;
                output "String";
                output Line("Hey", 1);
            }
            """;

        Language.Compiler compiler = new(code);

        CompilationResult result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        _ = DynamicCompiler.CompileToStory(result.CSharpText);
    }

    [TestMethod]
    public void TestEmptyStory()
    {
        string code =
            """
            scene main { }
            """;

        Language.Compiler compiler = new(code);

        CompilationResult result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        IStory<int, int> story = DynamicCompiler.CompileToStory<int, int>(result.CSharpText);

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

        Language.Compiler compiler = new(code);
        CompilationResult result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);

        IStory<int, int> story = DynamicCompiler.CompileToStory<int, int>(result.CSharpText!);

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
}
