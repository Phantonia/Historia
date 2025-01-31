using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private (IEnumerable<SubroutineSymbol>? topologicalOrder, IReadOnlyDictionary<SubroutineSymbol, int> referenceCounts) PerformDependencyAnalysis(IReadOnlyDictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs)
    {
        Dictionary<long, IReadOnlySet<long>> dependencies = [];
        Dictionary<long, Symbol> symbols = [];
        Dictionary<long, int> referenceCounts = [];

        foreach (SubroutineSymbol subroutine in subroutineFlowGraphs.Keys)
        {
            referenceCounts[subroutine.Index] = 0;
        }

        foreach ((SubroutineSymbol subroutine, FlowGraph flowGraph) in subroutineFlowGraphs)
        {
            IReadOnlyDictionary<long, int> theseDependenciesAndReferenceCounts = GetDependenciesAndReferenceCounts(flowGraph);
            dependencies[subroutine.Index] = (SortedSet<long>)[.. theseDependenciesAndReferenceCounts.Keys];
            symbols[subroutine.Index] = subroutine;

            foreach ((long dep, int refCount) in theseDependenciesAndReferenceCounts)
            {
                referenceCounts.TryAdd(dep, 0);
                referenceCounts[dep] += refCount;
            }
        }

        foreach (SubroutineSymbol subroutine in subroutineFlowGraphs.Keys)
        {
            if (subroutine.IsChapter && subroutine.Name is not "main" && referenceCounts[subroutine.Index] != 1)
            {
                ErrorFound?.Invoke(Errors.ChapterMustBeCalledExactlyOnce(subroutine.Name, referenceCounts[subroutine.Index], subroutine.Index));
            }
        }

        DependencyGraph dependencyGraph = new()
        {
            Dependencies = dependencies,
            Symbols = symbols,
        };

        Dictionary<SubroutineSymbol, int> finalReferenceCounts = referenceCounts.ToDictionary(p => (SubroutineSymbol)dependencyGraph.Symbols[p.Key], p => p.Value);

        // spec 1.2.2 "No subroutine may ever directly or indirectly depend on itself."
        if (dependencyGraph.IsCyclic(out IEnumerable<long>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicSubroutineDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));
            return (null, finalReferenceCounts);
        }

        IEnumerable<SubroutineSymbol> topologicalOrder =
            dependencyGraph.TopologicalSort()
                           .Select(i => (SubroutineSymbol)dependencyGraph.Symbols[i])
                           .SkipWhile(s => s.Name != "main"); // when we have uncalled subroutines they might appear before "main" here. we can just ignore them

        return (topologicalOrder, finalReferenceCounts);
    }

    private static IReadOnlyDictionary<long, int> GetDependenciesAndReferenceCounts(FlowGraph flowGraph)
    {
        Dictionary<long, int> referenceCounts = [];

        foreach (FlowVertex vertex in flowGraph.Vertices.Values)
        {
            // skip purely semantic vertices
            if (vertex.IsStory && vertex.AssociatedStatement is BoundCallStatementNode { Subroutine: SubroutineSymbol calledSubroutine })
            {
                referenceCounts.TryAdd(calledSubroutine.Index, 0);
                referenceCounts[calledSubroutine.Index]++;
            }
        }

        return referenceCounts;
    }
}
