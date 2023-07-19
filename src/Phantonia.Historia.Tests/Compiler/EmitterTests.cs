using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System;

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
}
