using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class DependencyGraphTests
{
    [TestMethod]
    public void TestCyclicGraph()
    {
        Dictionary<long, Symbol> symbols = new()
        {
            [17] = new PseudoRecordTypeSymbol { Name = "Abc", Properties = [], IsLineRecord = false, Index = 17, },
            [26] = new PseudoRecordTypeSymbol { Name = "Def", Properties = [], IsLineRecord = false, Index = 26, },
            [43] = new PseudoRecordTypeSymbol { Name = "Ghi", Properties = [], IsLineRecord = false, Index = 43, },
            [55] = new PseudoRecordTypeSymbol { Name = "Jkl", Properties = [], IsLineRecord = false, Index = 55, },
            [70] = new PseudoRecordTypeSymbol { Name = "Mno", Properties = [], IsLineRecord = false, Index = 70, },
            [89] = new PseudoRecordTypeSymbol { Name = "Pqr", Properties = [], IsLineRecord = false, Index = 89, },
            [105] = new PseudoRecordTypeSymbol { Name = "Stu", Properties = [], IsLineRecord = false, Index = 105, },
            [117] = new PseudoRecordTypeSymbol { Name = "Vw", Properties = [], IsLineRecord = false, Index = 117, }, // no ad
            [123] = new PseudoRecordTypeSymbol { Name = "Xyz", Properties = [], IsLineRecord = false, Index = 123, },
        };

        Dictionary<long, IReadOnlySet<long>> dependencies = new()
        {
            [17] = (SortedSet<long>)[26, 43],
            [26] = (SortedSet<long>)[43, 55],
            [43] = (SortedSet<long>)[70],
            [55] = (SortedSet<long>)[70, 89],
            [70] = (SortedSet<long>)[105],
            [89] = (SortedSet<long>)[105, 117],
            [105] = (SortedSet<long>)[117],
            [117] = (SortedSet<long>)[123],
            [123] = (SortedSet<long>)[89],
        };

        DependencyGraph graph = new()
        {
            Symbols = symbols,
            Dependencies = dependencies,
        };

        Assert.IsTrue(graph.IsCyclic(out IEnumerable<long>? foundCycle));

        List<long> cycle = foundCycle.Distinct().ToList();

        switch (cycle.Count)
        {
            case 3:
                Assert.IsTrue(cycle.OrderBy(i => i).SequenceEqual([89, 117, 123]));
                break;
            case 4:
                Assert.IsTrue(cycle.OrderBy(i => i).SequenceEqual([89, 105, 117, 123]));
                break;
            default:
                Assert.Fail("Cycle is not of length 3 or 4");
                break;
        }
    }

    //[TestMethod]
    public void TestAcyclicGraph()
    {
        Dictionary<long, Symbol> symbols = new()
        {
            [17] = new PseudoRecordTypeSymbol { Name = "Abc", Properties = [], IsLineRecord = false, Index = 17, },
            [26] = new PseudoRecordTypeSymbol { Name = "Def", Properties = [], IsLineRecord = false, Index = 26, },
            [43] = new PseudoRecordTypeSymbol { Name = "Ghi", Properties = [], IsLineRecord = false, Index = 43, },
            [55] = new PseudoRecordTypeSymbol { Name = "Jkl", Properties = [], IsLineRecord = false, Index = 55, },
            [70] = new PseudoRecordTypeSymbol { Name = "Mno", Properties = [], IsLineRecord = false, Index = 70, },
            [89] = new PseudoRecordTypeSymbol { Name = "Pqr", Properties = [], IsLineRecord = false, Index = 89, },
            [105] = new PseudoRecordTypeSymbol { Name = "Stu", Properties = [], IsLineRecord = false, Index = 105, },
            [117] = new PseudoRecordTypeSymbol { Name = "Vw", Properties = [], IsLineRecord = false, Index = 117, }, // no ad
            [123] = new PseudoRecordTypeSymbol { Name = "Xyz", Properties = [], IsLineRecord = false, Index = 123, },
        };

        Dictionary<long, IReadOnlySet<long>> dependencies = new()
        {
            [17] = (SortedSet<long>)[26, 43],
            [26] = (SortedSet<long>)[43, 55],
            [43] = (SortedSet<long>)[70],
            [55] = (SortedSet<long>)[70, 89],
            [70] = (SortedSet<long>)[105],
            [89] = (SortedSet<long>)[105, 117],
            [105] = (SortedSet<long>)[117],
            [117] = (SortedSet<long>)[123],
            [123] = (SortedSet<long>)[],
        };

        DependencyGraph graph = new()
        {
            Symbols = symbols,
            Dependencies = dependencies,
        };

        Assert.IsFalse(graph.IsCyclic(out IEnumerable<long>? cycle));
        Assert.IsNull(cycle);

        IEnumerable<long> topologicalOrdering = graph.GetDependencyRespectingOrder();

        // only in our specific case do we assume that a vertex only points at higher indexed vertices
        // in reality this might not be the case
        // however, we start our ordering with the vertex without any outgoing edges
        Assert.IsTrue(topologicalOrdering.Order().SequenceEqual(topologicalOrdering));
    }
}
