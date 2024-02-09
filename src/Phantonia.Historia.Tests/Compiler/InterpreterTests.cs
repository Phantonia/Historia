using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System.Collections.Immutable;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class InterpreterTests
{
    [TestMethod]
    public void TestInterpreter()
    {
        string code =
            """
            record X(A: String, B: Int);
            enum Y(A, B, C);
            union Z(X, Y, Int);

            setting OutputType: Z;
            setting OptionType: Z;

            outcome A(X, Y);
            spectrum B(X <= 1/2, Y);

            scene main
            {
                outcome C(X, Y) default X;
                spectrum D(X < 1/3, Y) default Y;

                output Y.A;
                output X("string", 42);

                switch (0)
                {
                    option (100)
                    {
                        A = X;
                        strengthen B by 15;
                    }

                    option (101)
                    {
                        A = Y;
                        weaken B by 15;
                    }

                    option (102)
                    {
                        A = X;
                        strengthen B by 8;
                        weaken B by 7;
                    }
                }

                output 1;

                branchon A
                {
                    option X
                    {
                        output 2;
                    }

                    option Y
                    {
                         output 3;   
                    }
                }

                branchon B
                {
                    option X
                    {
                        branchon C
                        {
                            option X
                            {
                                output 4;
                            }

                            option Y
                            {
                                output 5;
                            }
                        }
                    }

                    other
                    {
                        branchon D
                        {
                            option X
                            {
                                output 6;
                            }

                            option Y
                            {
                                output 7;
                            }
                        }
                    }
                }

                loop switch (8)
                {
                    loop option (200)
                    {
                        output 9;
                    }

                    option (201)
                    {
                        output 10;
                    }

                    option (202)
                    {
                        output 11;
                    }

                    final option (203)
                    {
                        output 12;
                    }
                }
            }
            """;

        Interpreter intp = new(code);
        InterpretationResult result = intp.Interpret();

        Assert.IsTrue(result.IsValid);

        InterpreterStateMachine sm = result.StateMachine;

        void AssertContinue() => Assert.IsTrue(sm.TryContinue());
        void AssertContinueWithOption(int option) => Assert.IsTrue(sm.TryContinueWithOption(option));

        // playthrough 1
        {
            Assert.IsTrue(sm.NotStartedStory);
            Assert.IsFalse(sm.FinishedStory);
            Assert.IsNull(sm.Output);
            Assert.AreEqual(0, sm.Options.Count);

            Assert.IsFalse(sm.TryContinueWithOption(0));
            AssertContinue();
            Assert.AreEqual("Y.A", sm.Output);
            Assert.AreEqual(0, sm.Options.Count);

            AssertContinue();
            Assert.IsTrue(sm.Output is RecordInstance
            {
                RecordName: "X",
                Properties: ImmutableDictionary<string, object> dict,
            } && (string)dict["A"] == "string" && (int)dict["B"] == 42);

            AssertContinue();
            Assert.AreEqual(0, (int)sm.Output);
            Assert.AreEqual(3, sm.Options.Count);
            Assert.AreEqual(100, (int)sm.Options[0]);

            Assert.IsFalse(sm.TryContinue());
            AssertContinueWithOption(0);
            Assert.AreEqual(1, (int)sm.Output);

            AssertContinue();
            Assert.AreEqual(2, (int)sm.Output);

            AssertContinue();
            Assert.AreEqual(7, (int)sm.Output);

            AssertContinue();
            Assert.AreEqual(8, (int)sm.Output);
            Assert.AreEqual(4, sm.Options.Count);
            Assert.AreEqual(202, (int)sm.Options[2]);

            AssertContinueWithOption(1);
            Assert.AreEqual(10, (int)sm.Output);

            AssertContinue();
            Assert.AreEqual(8, (int)sm.Output);
            Assert.AreEqual(3, sm.Options.Count);

            for (int i = 0; i < 3; i++)
            {
                AssertContinueWithOption(0);
                Assert.AreEqual(9, (int)sm.Output);
                AssertContinue();
            }

            AssertContinueWithOption(2);
            Assert.AreEqual(12, (int)sm.Output);

            AssertContinue();
            Assert.IsTrue(sm.FinishedStory);
            Assert.IsFalse(sm.TryContinue());
            Assert.IsFalse(sm.TryContinueWithOption(0));
        }
    }
}
