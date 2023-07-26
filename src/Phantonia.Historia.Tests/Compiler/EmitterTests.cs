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

    // this is copy pasted
    // in the long run why might want to test with dynamically compiled classes
    public sealed class HistoriaStory
    {
        public HistoriaStory()
        {
            Output = 16;
        }

        private int state = 19;

        public bool FinishedStory { get; private set; } = false;

        public System.Collections.Immutable.ImmutableArray<int> Options { get; private set; } = System.Collections.Immutable.ImmutableArray<int>.Empty;

        public int Output { get; private set; }

        public bool TryContinue()
        {
            if (FinishedStory || Options.Length != 0)
            {
                return false;
            }

            state = GetNextState(0);
            Output = GetOutput();
            Options = GetOptions();

            if (state == -1)
            {
                FinishedStory = true;
            }

            return true;
        }

        public bool TryContinueWithOption(int option)
        {
            if (FinishedStory || option < 0 || option >= Options.Length)
            {
                return false;
            }

            state = GetNextState(option);
            Output = GetOutput();
            Options = GetOptions();

            if (state == -1)
            {
                FinishedStory = true;
            }

            return true;
        }

        private int GetNextState(int option)
        {
            switch (state, option)
            {
                case (19, _):
                    return 43;
                case (43, 0):
                    return 107;
                case (43, 1):
                    return 204;
                case (107, _):
                    return 133;
                case (133, _):
                    return 452;
                case (204, _):
                    return 232;
                case (232, 0):
                    return 328;
                case (232, 1):
                    return 452;
                case (328, _):
                    return 452;
                case (452, _):
                    return -1;
            }

            throw new System.InvalidOperationException("Invalid state");
        }

        private int GetOutput()
        {
            switch (state)
            {
                case 19:
                    return 16;
                case 43:
                    return 17;
                case 107:
                    return 19;
                case 133:
                    return 20;
                case 204:
                    return 22;
                case 232:
                    return 23;
                case 328:
                    return 25;
                case 452:
                    return 27;
                case -1:
                    return default;
            }

            throw new System.InvalidOperationException("Invalid state");
        }

        private System.Collections.Immutable.ImmutableArray<int> GetOptions()
        {
            switch (state)
            {
                case 43:
                    return System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { 18, 21, });
                case 232:
                    return System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { 24, 26, });
            }

            return System.Collections.Immutable.ImmutableArray<int>.Empty;
        }
    }

    [TestMethod]
    public void TestEmittedCode()
    {
        /*
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
        */

        HistoriaStory story = new();

        // output (16);
        Assert.AreEqual(16, story.Output);
        Assert.AreEqual(0, story.Options.Length);

        // switch (17)
        Assert.IsFalse(story.TryContinueWithOption(0));
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(17, story.Output);
        Assert.AreEqual(2, story.Options.Length);
        Assert.AreEqual(18, story.Options[0]);
        Assert.AreEqual(21, story.Options[1]);

        // option (18), output (19);
        Assert.IsFalse(story.TryContinue());
        Assert.IsFalse(story.TryContinueWithOption(-1));
        Assert.IsFalse(story.TryContinueWithOption(2));
        Assert.IsFalse(story.TryContinueWithOption(int.MaxValue));
        Assert.IsTrue(story.TryContinueWithOption(0));
        Assert.AreEqual(19, story.Output);
        Assert.AreEqual(0, story.Options.Length);

        // output (20);
        for (int i = 0; i < 10; i++)
        {
            Assert.IsFalse(story.TryContinueWithOption(Random.Shared.Next()));
            Assert.IsFalse(story.TryContinueWithOption(-Random.Shared.Next()));
        }
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(20, story.Output);
        Assert.AreEqual(0, story.Options.Length);

        // output (27);
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(27, story.Output);
        Assert.AreEqual(0, story.Options.Length);

        // done
        Assert.IsTrue(story.TryContinue());
        Assert.AreEqual(default, story.Output);
        Assert.IsTrue(story.FinishedStory);
        Assert.AreEqual(0, story.Options.Length);

        Assert.IsFalse(story.TryContinue());
        Assert.IsFalse(story.TryContinueWithOption(0));
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

        var result = compiler.CompileToCSharpText();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);
        Assert.IsNotNull(result.CSharpText);
    }
}
