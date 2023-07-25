using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.LexicalAnalysis;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class StringTests
{
    [TestMethod]
    public void TestStringParsing()
    {
        string literal =
            """
            abc\r\ndef\\\&
            """;

        string expectedValue =
            """
            abc
            def\&
            """;

        string actualValue = StringParser.Parse(literal);

        Assert.AreEqual(expectedValue, actualValue);
    }
}
