using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using Phantonia.Historia.Language.Refactorings;

namespace Phantonia.Historia.Tests.Refactorings;

[TestClass]
public sealed class LineRefactoringTests
{
    [TestMethod]
    public void TestLineRefactoring()
    {
        string original =
            """"
            enum Character(Alice, Bob);
            line record Line(Character: Character, Text: String);
            line record TaggedLine(Character: String, Tag: Int, Text: String);
            union Output(Line, TaggedLine, Int);

            setting OutputType: Output;
            setting OptionType: String;

            chapter main
            {
                output Line(Character.Alice, "Hello world");
                output TaggedLine("Charlie", 62, "Goodbye world");
                
                switch 0
                {
                    option "abc"
                    {
                        output Line(Character.Bob, '"Blablabla"');
                    }

                    option "xyz"
                    {
                        output TaggedLine("Danielle", 5, 'uwu');
                    }
                }
            }
            """";

        string refactored =
            """"
            enum Character(Alice, Bob);
            line record Line(Character: Character, Text: String);
            line record TaggedLine(Character: String, Tag: Int, Text: String);
            union Output(Line, TaggedLine, Int);
            
            setting OutputType: Output;
            setting OptionType: String;
            
            chapter main
            {
                Alice: "Hello world";
                "Charlie" [62]: "Goodbye world";
                
                switch 0
                {
                    option "abc"
                    {
                        Bob: '"Blablabla"';
                    }
            
                    option "xyz"
                    {
                        "Danielle" [5]: 'uwu';
                    }
                }
            }
            """";

        string result = Refactory.RefactorString(original, new LineRefactoring());
        Assert.AreEqual(refactored, result);
    }
}
