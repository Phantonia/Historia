using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.SyntaxAnalysis;

namespace Phantonia.Historia.Tests.Compiler;

public static class NodeAssert
{
    public static void ReconstructWorks(string code, StoryNode tree)
    {
        string reconstructedCode = tree.Reconstruct();
        Assert.AreEqual(code, reconstructedCode);
    }
}
