using Phantonia.Historia.Language.Ast;
using Phantonia.Historia.Language.Ast.Statements;
using Phantonia.Historia.Language.Ast.Symbols;
using Phantonia.Historia.Language.Flow;
using System;
using System.Diagnostics;

namespace Phantonia.Historia.Language;

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
        foreach (SymbolDeclarationNode symbolDeclaration in story.Symbols)
        {
            if (symbolDeclaration is SceneSymbolDeclarationNode { Name: "main" } mainScene)
            {
                return GenerateBodyFlowGraph(mainScene.Body);
            }
        }

        Debug.Assert(false); // we don't have a main scene - should have been caught by the binder already
        return null;
    }

    private FlowGraph GenerateBodyFlowGraph(SceneBodyNode body)
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
            OutputStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex { Index = statement.Index }),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }
}
