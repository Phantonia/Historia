using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using System;
using System.Diagnostics;

namespace Phantonia.Historia.Language.FlowAnalysis;

// there is no abbreviation for this thing
// always use its full name, im serious
public sealed class FlowAnalyzer
{
    public FlowAnalyzer(StoryNode story)
    {
        this.story = story;
    }

    private readonly StoryNode story;

    public FlowGraph GenerateMainFlowGraph()
    {
        foreach (TopLevelNode symbolDeclaration in story.TopLevelNodes)
        {
            if (symbolDeclaration is SceneSymbolDeclarationNode { Name: "main" } mainScene)
            {
                return GenerateBodyFlowGraph(mainScene.Body);
            }
        }

        Debug.Assert(false); // we don't have a main scene - should have been caught by the binder already
        return null;
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
            OutputStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = statement.Index, AssociatedStatement = statement }),
            SwitchStatementNode switchStatement => GenerateSwitchFlowGraph(switchStatement),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }

    private FlowGraph GenerateSwitchFlowGraph(SwitchStatementNode switchStatement)
    {
        FlowGraph flowGraph = FlowGraph.Empty.AddVertex(new FlowVertex { Index = switchStatement.Index, AssociatedStatement = switchStatement });

        foreach (OptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body);

            flowGraph = flowGraph.AppendToVertex(flowGraph.StartVertex, nestedFlowGraph);
        }

        return flowGraph;
    }
}
