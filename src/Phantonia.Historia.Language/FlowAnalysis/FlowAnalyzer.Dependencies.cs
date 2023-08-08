using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed partial class FlowAnalyzer
{
    private IReadOnlyDictionary<SceneSymbol, int> GetSceneReferenceCounts(IReadOnlyDictionary<SceneSymbol, FlowGraph> sceneFlowGraphs)
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
                if (referenceCounts.ContainsKey(dep))
                {
                    referenceCounts[dep]++;
                }
                else
                {
                    referenceCounts[dep] = 1;
                }
            }
        }

        DependencyGraph dependencyGraph = new()
        {
            Dependencies = dependencies,
            Symbols = symbols,
        };

        if (dependencyGraph.IsCyclic(out IEnumerable<int>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicSceneDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));
        }

        return referenceCounts.ToDictionary(p => (SceneSymbol)dependencyGraph.Symbols[p.Key], p => p.Value);
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
