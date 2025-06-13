using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

// there is no abbreviation for this thing
// always use its full name, i'm serious
public sealed partial class FlowAnalyzer(StoryNode story, SymbolTable symbolTable)
{
    public event Action<Error>? ErrorFound;

    public FlowAnalysisResult PerformFlowAnalysis()
    {
        Dictionary<SubroutineSymbol, FlowGraph> subroutineFlowGraphs = [];

        uint vertexIndex = 1;

        foreach (TopLevelNode symbolDeclaration in story.GetTopLevelNodes())
        {
            if (symbolDeclaration is BoundSymbolDeclarationNode
                {
                    Original: SubroutineSymbolDeclarationNode
                    {
                        Body: StatementBodyNode body,
                    },
                    Symbol: SubroutineSymbol subroutine,
                })
            {
                FlowGraph subroutineFlowGraph = GenerateBodyFlowGraph(body, ref vertexIndex);
                subroutineFlowGraphs[subroutine] = subroutineFlowGraph;
            }
        }

        SubroutineSymbol mainSubroutine = (SubroutineSymbol)symbolTable["main"];

        (IEnumerable<SubroutineSymbol>? topologicalOrder, IReadOnlyDictionary<SubroutineSymbol, int> referenceCounts) = PerformDependencyAnalysis(subroutineFlowGraphs, mainSubroutine);

        if (topologicalOrder is null)
        {
            return new FlowAnalysisResult
            {
                MainFlowGraph = null,
                SymbolTable = null,
                ChapterData = null,
            };
        }

        PerformReachabilityAnalysis(subroutineFlowGraphs, out ImmutableDictionary<SubroutineSymbol, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtChapters);

        (FlowGraph mainFlowGraph, SymbolTable updatedSymbolTable, ImmutableDictionary<SubroutineSymbol, uint>? chapterEntryVertices) = MergeFlowGraphs(topologicalOrder, subroutineFlowGraphs, referenceCounts, ref vertexIndex);

        ImmutableDictionary<SubroutineSymbol, ChapterData> chapterData = definitelyAssignedOutcomesAtChapters.Select(kvp => KeyValuePair.Create(kvp.Key, new ChapterData
        {
            DefinitelyAssignedOutcomes = kvp.Value,
            EntryVertex = chapterEntryVertices[kvp.Key]
        })).ToImmutableDictionary();

        return new FlowAnalysisResult
        {
            MainFlowGraph = mainFlowGraph,
            SymbolTable = updatedSymbolTable,
            ChapterData = chapterData,
        };
    }

    private FlowGraph GenerateBodyFlowGraph(StatementBodyNode body, ref uint vertexIndex)
    {
        FlowGraph graph = FlowGraph.Empty;

        foreach (StatementNode statement in body.Statements)
        {
            FlowGraph statementGraph = GenerateStatementFlowGraph(statement, ref vertexIndex);
            graph = graph.Append(statementGraph);
        }

        graph = UpdateLoopSwitchLastEdges(graph);

        return graph;
    }

    private static FlowGraph UpdateLoopSwitchLastEdges(FlowGraph graph)
    {
        // the flow branching statement for loop switches always ends in a final edge because it is not updated in FlowGraph.Append
        // so we do that here

        foreach (FlowVertex vertex in graph.Vertices.Values)
        {
            if (vertex.AssociatedStatement is not FlowBranchingStatementNode { Original: LoopSwitchStatementNode, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges } flowBranchingStatement)
            {
                continue;
            }

            FlowEdge lastEdge = graph.OutgoingEdges[vertex.Index][^1];

            if (lastEdge != FlowGraph.FinalEdge)
            {
                graph = graph.SetVertex(vertex.Index, vertex with
                {
                    AssociatedStatement = flowBranchingStatement with
                    {
                        OutgoingEdges = [.. outgoingEdges.RemoveAt(outgoingEdges.Count - 1), lastEdge],
                    }
                });
            }
        }

        return graph;
    }

    private FlowGraph GenerateStatementFlowGraph(StatementNode statement, ref uint vertexIndex)
    {
        return statement switch
        {
            OutputStatementNode or CallStatementNode or BoundLineStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = vertexIndex++,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Visible,
            }),
            SwitchStatementNode switchStatement => GenerateSwitchFlowGraph(switchStatement, ref vertexIndex),
            LoopSwitchStatementNode loopSwitchStatement => GenerateLoopSwitchFlowGraph(loopSwitchStatement, ref vertexIndex),
            BranchOnStatementNode branchOnStatement => GenerateBranchOnFlowGraph(branchOnStatement, ref vertexIndex),
            OutcomeDeclarationStatementNode => FlowGraph.Empty,
            SpectrumDeclarationStatementNode => FlowGraph.Empty,
            AssignmentStatementNode or SpectrumAdjustmentStatementNode or BoundRunStatementNode => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = vertexIndex++,
                AssociatedStatement = statement,
                Kind = FlowVertexKind.Invisible,
            }),
            BoundChooseStatementNode chooseStatement => GenerateChooseFlowGraph(chooseStatement, ref vertexIndex),
            IfStatementNode ifStatement => GenerateIfFlowGraph(ifStatement, ref vertexIndex),
            NoOpStatementNode noopStatement => FlowGraph.CreateSimpleFlowGraph(new FlowVertex
            {
                Index = vertexIndex++,
                AssociatedStatement = noopStatement,
                Kind = FlowVertexKind.Invisible,
            }),
            _ => throw new NotImplementedException($"Unknown statement type {statement.GetType().FullName}"),
        };
    }

    private FlowGraph GenerateSwitchFlowGraph(SwitchStatementNode switchStatement, ref uint vertexIndex)
    {
        uint switchVertexIndex = vertexIndex++;

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = switchVertexIndex,
            AssociatedStatement = switchStatement,
            Kind = FlowVertexKind.Visible,
        });

        FlowGraph bodyFlowGraph = GenerateBodyFlowGraph(switchStatement.Body, ref vertexIndex);
        flowGraph = flowGraph.Append(bodyFlowGraph);

        foreach (OptionNode option in switchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body, ref vertexIndex);
            flowGraph = flowGraph.AppendToVertex(switchVertexIndex, nestedFlowGraph);

            foreach (FlowVertex vertex in bodyFlowGraph.Vertices.Values)
            {
                if (!vertex.IsVisible || !vertex.IsStory)
                {
                    continue;
                }

                foreach (FlowEdge nestedStartEdge in nestedFlowGraph.StartEdges)
                {
                    flowGraph = flowGraph.AddEdge(vertex.Index, nestedStartEdge);
                }
            }
        }

        ImmutableArray<ExpressionNode> optionExpressions = switchStatement.Options.Select(o => o.Expression).ToImmutableArray();

        foreach (FlowVertex vertex in bodyFlowGraph.Vertices.Values)
        {
            if (!vertex.IsVisible || !vertex.IsStory)
            {
                if (bodyFlowGraph.OutgoingEdges[vertex.Index].Any(e => e.ToVertex is FlowGraph.Sink))
                {
                    ErrorFound?.Invoke(Errors.SwitchBodyEndsInInvisibleStatement(vertex.AssociatedStatement.Index));
                }

                continue;
            }

            FlowEdge? nonOptionEdge = bodyFlowGraph.OutgoingEdges[vertex.Index].Single();

            ImmutableList<FlowEdge> outgoingEdges = flowGraph.OutgoingEdges[vertex.Index];
            outgoingEdges = outgoingEdges.Remove((FlowEdge)nonOptionEdge);

            if (((FlowEdge)nonOptionEdge).ToVertex is FlowGraph.Sink)
            {
                nonOptionEdge = null;
            }

            DynamicSwitchFlowBranchingStatementNode flowBranchingStatement = new()
            {
                Original = vertex.AssociatedStatement,
                OptionExpressions = optionExpressions,
                NonOptionEdge = nonOptionEdge,
                OutgoingEdges = outgoingEdges,
                Index = vertex.Index,
                PrecedingTokens = [],
            };

            flowGraph = flowGraph.SetVertex(vertex.Index, flowGraph.Vertices[vertex.Index] with
            {
                AssociatedStatement = flowBranchingStatement,
            });
        }

        {
            FlowEdge? nonOptionEdge = !bodyFlowGraph.Vertices.IsEmpty ? bodyFlowGraph.StartEdges.Single(e => e.IsStory) : null;
            ImmutableList<FlowEdge> outgoingEdges = flowGraph.OutgoingEdges[switchVertexIndex];
            FlowBranchingStatementNode flowBranchingStatement;

            if (nonOptionEdge is not null)
            {
                outgoingEdges = outgoingEdges.Remove((FlowEdge)nonOptionEdge);

                flowBranchingStatement = new DynamicSwitchFlowBranchingStatementNode
                {
                    Original = switchStatement,
                    OptionExpressions = optionExpressions,
                    NonOptionEdge = (FlowEdge)nonOptionEdge,
                    OutgoingEdges = outgoingEdges,
                    Index = switchStatement.Index,
                    PrecedingTokens = [],
                };
            }
            else
            {
                flowBranchingStatement = new FlowBranchingStatementNode
                {
                    Original = switchStatement,
                    OutgoingEdges = outgoingEdges,
                    Index = switchStatement.Index,
                    PrecedingTokens = [],
                };
            }

            flowGraph = flowGraph.SetVertex(switchVertexIndex, flowGraph.Vertices[switchVertexIndex] with
            {
                AssociatedStatement = flowBranchingStatement,
            });
        }

        return flowGraph;
    }

    private FlowGraph GenerateLoopSwitchFlowGraph(LoopSwitchStatementNode loopSwitchStatement, ref uint vertexIndex)
    {
        bool hasFinalOption = loopSwitchStatement.Options.Any(o => o.Kind == LoopSwitchOptionKind.Final);

        uint loopSwitchVertexIndex = vertexIndex++;

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = loopSwitchVertexIndex,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.Visible,
        });

        uint copyVertexIndex = vertexIndex++;

        FlowGraph semanticCopy = FlowGraph.CreateSimpleSemanticFlowGraph(new FlowVertex
        {
            Index = copyVertexIndex,
            AssociatedStatement = loopSwitchStatement,
            Kind = FlowVertexKind.PurelySemantic,
        });

        List<uint> nonFinalEndVertices = [];
        List<uint> finalStartVertices = [];

        foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body, ref vertexIndex);

            flowGraph = flowGraph.AppendToVertex(loopSwitchVertexIndex, nestedFlowGraph);

            FlowGraph nestedSemanticCopy = CreateSemanticCopy(nestedFlowGraph, ref vertexIndex);
            semanticCopy = semanticCopy.AppendToVertex(copyVertexIndex, nestedSemanticCopy);

            if (option.Kind != LoopSwitchOptionKind.Final)
            {
                // redirect final edges as weak edges back up
                foreach (uint vertex in nestedFlowGraph.OutgoingEdges.Where(p => p.Value.Contains(FlowGraph.FinalEdge)).Select(p => p.Key))
                {
                    flowGraph = flowGraph.ReplaceEdge(vertex, FlowGraph.Sink, FlowEdge.CreateWeakTo(loopSwitchVertexIndex)); // important: weak edge
                    
                    nonFinalEndVertices.Add(vertex);
                }
            }
            else
            {
                foreach (FlowEdge edge in nestedFlowGraph.StartEdges)
                {
                    finalStartVertices.Add(edge.ToVertex);
                }
            }
        }

        foreach (uint nonFinalEndVertex in nonFinalEndVertices)
        {
            if (hasFinalOption)
            {
                // add purely semantic edge from all end vertices of non final options to all start vertices of final options
                foreach (uint finalStartVertex in finalStartVertices)
                {
                    flowGraph = flowGraph.AddEdge(nonFinalEndVertex, FlowEdge.CreatePurelySemanticTo(finalStartVertex));
                }
            }
            else
            {
                // add purely semantic edges from all end vertices of non final options into nothingness
                flowGraph = flowGraph.AddEdge(nonFinalEndVertex, FlowEdge.CreatePurelySemanticTo(FlowGraph.Sink));
            }
        }

        // loop switches without final options automatically continue after all normal options have been selected
        if (!hasFinalOption)
        {
            flowGraph = flowGraph.AddEdge(loopSwitchVertexIndex, FlowGraph.FinalEdge);
        }

        Debug.Assert(flowGraph.IsConformable);
        uint startVertex = flowGraph.GetStoryStartVertex();

        flowGraph = semanticCopy.Append(flowGraph);
        flowGraph = flowGraph with
        {
            StartEdges = flowGraph.StartEdges.Add(FlowEdge.CreateWeakTo(startVertex)),
        };

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = loopSwitchStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[loopSwitchVertexIndex],
            Index = loopSwitchStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(loopSwitchVertexIndex, flowGraph.Vertices[loopSwitchVertexIndex] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph CreateSemanticCopy(FlowGraph flowGraph, ref uint vertexIndex)
    {
        FlowVertex SemantifyVertex(FlowVertex vertex, ref uint vertexIndex) => vertex with
        {
            Kind = FlowVertexKind.PurelySemantic,
            Index = vertex.Index != FlowGraph.Sink ? vertexIndex++ : FlowGraph.Sink,
        };

        Dictionary<uint, uint> originalToSemantic = [];
        FlowGraph flowGraphCopy = FlowGraph.Empty;

        foreach ((uint index, FlowVertex vertex) in flowGraph.Vertices)
        {
            FlowVertex copy = SemantifyVertex(vertex, ref vertexIndex);
            flowGraphCopy = flowGraphCopy.AddVertexWithoutStartEdge(copy);
            originalToSemantic[index] = copy.Index;
        }

        FlowEdge SemantifyEdge(FlowEdge edge)
        {
            FlowEdgeKind kind = edge.IsSemantic ? FlowEdgeKind.Semantic : FlowEdgeKind.None;

            if (edge.ToVertex == FlowGraph.Sink)
            {
                return edge with
                {
                    Kind = kind,
                };
            }
            else
            {
                return edge with
                {
                    Kind = kind,
                    ToVertex = originalToSemantic[edge.ToVertex],
                };
            }
        }

        foreach ((uint vertex, ImmutableList<FlowEdge> outgoingEdges) in flowGraph.OutgoingEdges)
        {
            foreach (FlowEdge edge in outgoingEdges)
            {
                flowGraphCopy = flowGraphCopy.AddEdge(originalToSemantic[vertex], SemantifyEdge(edge));
            }
        }

        foreach (FlowEdge edge in flowGraph.StartEdges)
        {
            flowGraphCopy = flowGraphCopy.AddStartEdge(SemantifyEdge(edge));
        }

        return flowGraphCopy;
    }

    private FlowGraph GenerateBranchOnFlowGraph(BranchOnStatementNode branchOnStatement, ref uint vertexIndex)
    {
        uint branchOnVertexIndex = vertexIndex++;

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = branchOnVertexIndex,
            AssociatedStatement = branchOnStatement,
            Kind = FlowVertexKind.Invisible,
        });

        foreach (BranchOnOptionNode option in branchOnStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body, ref vertexIndex);

            flowGraph = flowGraph.AppendToVertex(branchOnVertexIndex, nestedFlowGraph);
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = branchOnStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[branchOnVertexIndex],
            Index = branchOnStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(branchOnVertexIndex, flowGraph.Vertices[branchOnVertexIndex] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph GenerateChooseFlowGraph(BoundChooseStatementNode chooseStatement, ref uint vertexIndex)
    {
        uint chooseVertexIndex = vertexIndex++;

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = chooseVertexIndex,
            AssociatedStatement = chooseStatement,
            Kind = FlowVertexKind.Invisible,
        });

        foreach (OptionNode option in chooseStatement.Options)
        {
            FlowGraph nestedFlowGraph = GenerateBodyFlowGraph(option.Body, ref vertexIndex);

            flowGraph = flowGraph.AppendToVertex(chooseVertexIndex, nestedFlowGraph);
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = chooseStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[chooseVertexIndex],
            Index = chooseStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(chooseVertexIndex, flowGraph.Vertices[chooseVertexIndex] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }

    private FlowGraph GenerateIfFlowGraph(IfStatementNode ifStatement, ref uint vertexIndex)
    {
        uint ifVertexIndex = vertexIndex++;

        FlowGraph flowGraph = FlowGraph.CreateSimpleFlowGraph(new FlowVertex
        {
            Index = ifVertexIndex,
            AssociatedStatement = ifStatement,
            Kind = FlowVertexKind.Invisible,
        });

        FlowGraph thenFlowGraph = GenerateBodyFlowGraph(ifStatement.ThenBlock, ref vertexIndex);

        flowGraph = flowGraph.Append(thenFlowGraph);

        if (ifStatement.ElseBlock is not null)
        {
            FlowGraph elseFlowGraph = GenerateBodyFlowGraph(ifStatement.ElseBlock, ref vertexIndex);
            flowGraph = flowGraph.AppendToVertex(ifVertexIndex, elseFlowGraph);
        }
        else
        {
            // we need to draw an edge from the if statement vertex into nothingness
            flowGraph = flowGraph.AddEdge(ifVertexIndex, FlowEdge.CreateStrongTo(FlowGraph.Sink));
        }

        FlowBranchingStatementNode flowBranchingStatement = new()
        {
            Original = ifStatement,
            OutgoingEdges = flowGraph.OutgoingEdges[ifVertexIndex],
            Index = ifStatement.Index,
            PrecedingTokens = [],
        };

        flowGraph = flowGraph.SetVertex(ifVertexIndex, flowGraph.Vertices[ifVertexIndex] with
        {
            AssociatedStatement = flowBranchingStatement,
        });

        return flowGraph;
    }
}
