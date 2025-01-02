using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private (IEnumerable<SceneSymbol>? topologicalOrder, IReadOnlyDictionary<SceneSymbol, int> referenceCounts) PerformDependencyAnalysis(IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs)
    {
        Dictionary<long, IReadOnlySet<long>> dependencies = [];
        Dictionary<long, Symbol> symbols = [];
        Dictionary<long, int> referenceCounts = [];

        foreach ((SceneSymbol scene, FlowGraph flowGraph) in sceneFlowGraphs)
        {
            IReadOnlyDictionary<long, int> theseDependenciesAndReferenceCounts = GetDependenciesAndReferenceCounts(flowGraph);
            dependencies[scene.Index] = (SortedSet<long>)[.. theseDependenciesAndReferenceCounts.Keys];
            symbols[scene.Index] = scene;

            foreach ((long dep, int refCount) in theseDependenciesAndReferenceCounts)
            {
                referenceCounts.TryAdd(dep, 0);
                referenceCounts[dep] += refCount;
            }
        }

        DependencyGraph dependencyGraph = new()
        {
            Dependencies = dependencies,
            Symbols = symbols,
        };

        Dictionary<SceneSymbol, int> finalReferenceCounts = referenceCounts.ToDictionary(p => (SceneSymbol)dependencyGraph.Symbols[p.Key], p => p.Value);

        // spec 1.2.2 "No scene may ever directly or indirectly depend on itself."
        if (dependencyGraph.IsCyclic(out IEnumerable<long>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicSceneDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));
            return (null, finalReferenceCounts);
        }

        IEnumerable<SceneSymbol> topologicalOrder =
            dependencyGraph.TopologicalSort()
                           .Select(i => (SceneSymbol)dependencyGraph.Symbols[i])
                           .SkipWhile(s => s.Name != "main"); // when we have uncalled scenes they might appear before "main" here. we can just ignore them

        return (topologicalOrder, finalReferenceCounts);
    }

    private static IReadOnlyDictionary<long, int> GetDependenciesAndReferenceCounts(FlowGraph flowGraph)
    {
        Dictionary<long, int> referenceCounts = [];

        foreach (FlowVertex vertex in flowGraph.Vertices.Values)
        {
            // skip purely semantic vertices
            if (vertex.IsStory && vertex.AssociatedStatement is BoundCallStatementNode { Scene: SceneSymbol calledScene })
            {
                referenceCounts.TryAdd(calledScene.Index, 0);
                referenceCounts[calledScene.Index]++;
            }
        }

        return referenceCounts;
    }
}
