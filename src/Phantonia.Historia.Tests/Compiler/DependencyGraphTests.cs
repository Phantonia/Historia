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
            [17] = new RecordTypeSymbol { Name = "Abc", Properties = ImmutableArray<PropertySymbol>.Empty },
            [26] = new RecordTypeSymbol { Name = "Def", Properties = ImmutableArray<PropertySymbol>.Empty },
            [43] = new RecordTypeSymbol { Name = "Ghi", Properties = ImmutableArray<PropertySymbol>.Empty },
            [55] = new RecordTypeSymbol { Name = "Jkl", Properties = ImmutableArray<PropertySymbol>.Empty },
            [70] = new RecordTypeSymbol { Name = "Mno", Properties = ImmutableArray<PropertySymbol>.Empty },
            [89] = new RecordTypeSymbol { Name = "Pqr", Properties = ImmutableArray<PropertySymbol>.Empty },
            [105] = new RecordTypeSymbol { Name = "Stu", Properties = ImmutableArray<PropertySymbol>.Empty },
            [117] = new RecordTypeSymbol { Name = "Vw", Properties = ImmutableArray<PropertySymbol>.Empty }, // no ad
            [123] = new RecordTypeSymbol { Name = "Xyz", Properties = ImmutableArray<PropertySymbol>.Empty },
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

    [TestMethod]
    public void TestAcyclicGraph()
    {
        Dictionary<int, Symbol> symbols = new()
        {
            [17] = new RecordTypeSymbol { Name = "Abc", Properties = ImmutableArray<PropertySymbol>.Empty },
            [26] = new RecordTypeSymbol { Name = "Def", Properties = ImmutableArray<PropertySymbol>.Empty },
            [43] = new RecordTypeSymbol { Name = "Ghi", Properties = ImmutableArray<PropertySymbol>.Empty },
            [55] = new RecordTypeSymbol { Name = "Jkl", Properties = ImmutableArray<PropertySymbol>.Empty },
            [70] = new RecordTypeSymbol { Name = "Mno", Properties = ImmutableArray<PropertySymbol>.Empty },
            [89] = new RecordTypeSymbol { Name = "Pqr", Properties = ImmutableArray<PropertySymbol>.Empty },
            [105] = new RecordTypeSymbol { Name = "Stu", Properties = ImmutableArray<PropertySymbol>.Empty },
            [117] = new RecordTypeSymbol { Name = "Vw", Properties = ImmutableArray<PropertySymbol>.Empty }, // no ad
            [123] = new RecordTypeSymbol { Name = "Xyz", Properties = ImmutableArray<PropertySymbol>.Empty },
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
        Assert.IsTrue(topologicalOrdering.Order().SequenceEqual(topologicalOrdering.Reverse()));
    }
}
