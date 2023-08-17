using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.FlowAnalysis;

// there is no abbreviation for this thing
// always use its full name, i'm serious
public sealed partial class FlowAnalyzer
{
    public FlowAnalyzer(StoryNode story, SymbolTable symbolTable)
    {
        this.story = story;
        this.symbolTable = symbolTable;
    }

    private readonly StoryNode story;
    private readonly SymbolTable symbolTable;

    public event Action<Error>? ErrorFound;

    public FlowAnalysisResult PerformFlowAnalysis()
    {
        Dictionary<SceneSymbol, FlowGraph> sceneFlowGraphs = new();

        foreach (TopLevelNode symbolDeclaration in story.TopLevelNodes)
        {
            if (symbolDeclaration is BoundSymbolDeclarationNode
                {
                    Declaration: SceneSymbolDeclarationNode
                    {
                        Body: StatementBodyNode body,
                    },
                    Symbol: SceneSymbol scene,
                })
            {
                FlowGraph sceneFlowGraph = GenerateBodyFlowGraph(body);
                sceneFlowGraphs[scene] = sceneFlowGraph;
            }
        }

        (IEnumerable<SceneSymbol>? topologicalOrder, IReadOnlyDictionary<SceneSymbol, int> referenceCounts) = PerformDependencyAnalysis(sceneFlowGraphs);

        if (topologicalOrder is null)
        {
            return new FlowAnalysisResult
            {
                MainFlowGraph = null,
                SymbolTable = null,
            };
        }

        PerformReachabilityAnalysis(sceneFlowGraphs);

        (FlowGraph mainFlowGraph, SymbolTable updatedSymbolTable) = MergeFlowGraphs(topologicalOrder, sceneFlowGraphs, referenceCounts);

        return new FlowAnalysisResult
        {
            MainFlowGraph = mainFlowGraph,
            SymbolTable = updatedSymbolTable,
        };
    }

    private FlowGraph GenerateBodyFlowGraph(StatementBodyNode body)
    {
        FlowGraph graph = FlowGraph.Empty;

        foreach (StatementNode statement in body.Statements)
        {
            FlowGraph statementGraph = GenerateStatementFlowGraph(statement);
            graph = graph.Append(statementGraph);
        }

        return graph;
    }

    private FlowGraph GenerateStatementFlowGraph(StatementNode statement)
    {
        return statement switch
        {
            OutputStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                IsVisible = true,
            }),
            SwitchStatementNode switchStatement => GenerateSwitchFlowGraph(switchStatement),
            BranchOnStatementNode branchOnStatement => GenerateBranchOnFlowGraph(branchOnStatement),
            OutcomeDeclarationStatementNode => FlowGraph.Empty,
            SpectrumDeclarationStatementNode => FlowGraph.Empty,
            AssignmentStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                IsVisible = false,
            }),
            SpectrumAdjustmentStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                IsVisible = false,
            }),
            CallStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = statement.Index,
                AssociatedStatement = statement,
                IsVisible = true,
            }),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }

    private FlowGraph GenerateSwitchFlowGraph(SwitchStatementNode switchStatement)
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = switchStatement.Index,
            AssociatedStatement = switchStatement,
            IsVisible = true,
        });

        foreach (SwitchOptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);
        }

        return flowGraph;
    }

    private FlowGraph GenerateBranchOnFlowGraph(BranchOnStatementNode branchOnStatement)
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex
        {
            Index = branchOnStatement.Index,
            AssociatedStatement = branchOnStatement,
            IsVisible = false,
        });

        foreach (BranchOnOptionNode option in branchOnStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);
        }

        return flowGraph;
    }
}
