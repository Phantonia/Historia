using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language.SemanticAnalysis;
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
        Dictionary<int, Symbol> symbols = new()
        {
            [17] = new PseudoRecordTypeSymbol { Name = "Abc", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 17, },
            [26] = new PseudoRecordTypeSymbol { Name = "Def", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 26, },
            [43] = new PseudoRecordTypeSymbol { Name = "Ghi", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 43, },
            [55] = new PseudoRecordTypeSymbol { Name = "Jkl", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 55, },
            [70] = new PseudoRecordTypeSymbol { Name = "Mno", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 70, },
            [89] = new PseudoRecordTypeSymbol { Name = "Pqr", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 89, },
            [105] = new PseudoRecordTypeSymbol { Name = "Stu", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 105, },
            [117] = new PseudoRecordTypeSymbol { Name = "Vw", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 117, }, // no ad
            [123] = new PseudoRecordTypeSymbol { Name = "Xyz", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 123, },
        };

        Dictionary<int, IReadOnlyList<int>> dependencies = new()
        {
            [17] = new[] { 26, 43 },
            [26] = new[] { 43, 55 },
            [43] = new[] { 70 },
            [55] = new[] { 70, 89 },
            [70] = new[] { 105 },
            [89] = new[] { 105, 117 },
            [105] = new[] { 117 },
            [117] = new[] { 123 },
            [123] = new[] { 89 },
        };

        DependencyGraph graph = new()
        {
            Symbols = symbols,
            Dependencies = dependencies,
        };

        Assert.IsTrue(graph.IsCyclic(out IEnumerable<int>? foundCycle));

        List<int> cycle = foundCycle.Distinct().ToList();

        switch (cycle.Count)
        {
            case 3:
                Assert.IsTrue(cycle.OrderBy(i => i).SequenceEqual(new[] { 89, 117, 123 }));
                break;
            case 4:
                Assert.IsTrue(cycle.OrderBy(i => i).SequenceEqual(new[] { 89, 105, 117, 123 }));
                break;
            default:
                Assert.Fail("Cycle is not of length 3 or 4");
                break;
        }
    }

    //[TestMethod]
    public void TestAcyclicGraph()
    {
        Dictionary<int, Symbol> symbols = new()
        {
            [17] = new PseudoRecordTypeSymbol { Name = "Abc", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 17, },
            [26] = new PseudoRecordTypeSymbol { Name = "Def", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 26, },
            [43] = new PseudoRecordTypeSymbol { Name = "Ghi", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 43, },
            [55] = new PseudoRecordTypeSymbol { Name = "Jkl", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 55, },
            [70] = new PseudoRecordTypeSymbol { Name = "Mno", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 70, },
            [89] = new PseudoRecordTypeSymbol { Name = "Pqr", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 89, },
            [105] = new PseudoRecordTypeSymbol { Name = "Stu", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 105, },
            [117] = new PseudoRecordTypeSymbol { Name = "Vw", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 117, }, // no ad
            [123] = new PseudoRecordTypeSymbol { Name = "Xyz", Properties = ImmutableArray<PseudoPropertySymbol>.Empty, Index = 123, },
        };

        Dictionary<int, IReadOnlyList<int>> dependencies = new()
        {
            [17] = new[] { 26, 43 },
            [26] = new[] { 43, 55 },
            [43] = new[] { 70 },
            [55] = new[] { 70, 89 },
            [70] = new[] { 105 },
            [89] = new[] { 105, 117 },
            [105] = new[] { 117 },
            [117] = new[] { 123 },
            [123] = Array.Empty<int>(),
        };

        DependencyGraph graph = new()
        {
            Symbols = symbols,
            Dependencies = dependencies,
        };

        Assert.IsFalse(graph.IsCyclic(out IEnumerable<int>? cycle));
        Assert.IsNull(cycle);

        IEnumerable<int> topologicalOrdering = graph.TopologicalSort();

        // only in our specific case do we assume that a vertex only points at higher indexed vertices
        // in reality this might not be the case
        // however, we start our ordering with the vertex without any outgoing edges
        Assert.IsTrue(topologicalOrdering.Order().SequenceEqual(topologicalOrdering));
    }
}
