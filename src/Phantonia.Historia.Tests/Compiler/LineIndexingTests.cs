using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class LineIndexingTests
{
    [TestMethod]
    public void TestLineIndexing()
    {
        Dictionary<string, ImmutableArray<long>> pathLines = new()
        {
            ["abc"] = [12, 39, 56, 74],
            ["def"] = [86, 102, 109],
            ["xyz"] = [115, 118, 123, 132, 137],
        };

        LineIndexing indexing = new(pathLines.ToImmutableDictionary());

        LineCharacter lc = indexing.GetLineCharacter(64);
        Assert.AreEqual("abc", lc.Path);
        Assert.AreEqual(3, lc.Line);
        Assert.AreEqual(9, lc.Character);

        long index = indexing.GetIndex(lc);
        Assert.AreEqual(64, index);

        lc = new LineCharacter(3, 1, "xyz");
        index = indexing.GetIndex(lc);
        Assert.AreEqual(123, index);

        LineCharacter lc2 = indexing.GetLineCharacter(123);
        Assert.AreEqual(lc, lc2);
    }
}
