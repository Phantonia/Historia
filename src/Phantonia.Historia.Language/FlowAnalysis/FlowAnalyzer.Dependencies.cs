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
        Dictionary<int, IReadOnlyList<int>> dependencies = new();
        Dictionary<int, Symbol> symbols = new();
        Dictionary<int, int> referenceCounts = new();

        foreach ((SceneSymbol scene, FlowGraph flowGraph) in sceneFlowGraphs)
        {
            IReadOnlyList<int> theseDependencies = GetDependencies(scene, flowGraph);
            dependencies[scene.Index] = theseDependencies;
            symbols[scene.Index] = scene;

            foreach (int dep in theseDependencies)
            {
                referenceCounts.TryAdd(dep, 0);
                referenceCounts[dep]++;
            }
        }

        DependencyGraph dependencyGraph = new()
        {
            Dependencies = dependencies,
            Symbols = symbols,
        };

        Dictionary<SceneSymbol, int> finalReferenceCounts = referenceCounts.ToDictionary(p => (SceneSymbol)dependencyGraph.Symbols[p.Key], p => p.Value);

        // spec 1.2.2 "No scene may ever directly or indirectly depend on itself."
        if (dependencyGraph.IsCyclic(out IEnumerable<int>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicSceneDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));
            return (null, finalReferenceCounts);
        }

        IEnumerable<SceneSymbol> topologicalOrder = dependencyGraph.TopologicalSort().Select(i => (SceneSymbol)dependencyGraph.Symbols[i]);

        return (topologicalOrder, finalReferenceCounts);
    }

    private IReadOnlyList<int> GetDependencies(SceneSymbol scene, FlowGraph flowGraph)
    {
        HashSet<int> dependentScenes = new();

        foreach (FlowVertex vertex in flowGraph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is BoundCallStatementNode { Scene: SceneSymbol calledScene })
            {
                _ = dependentScenes.Add(calledScene.Index);
            }
        }

        return dependentScenes.ToList();
    }
}
