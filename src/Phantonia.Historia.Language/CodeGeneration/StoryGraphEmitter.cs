using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class StoryGraphEmitter(FlowGraph flowGraph, Settings settings, IndentedTextWriter writer)
{
    private readonly FlowGraph flowGraph = flowGraph.RemoveInvisible();

    public void GenerateStoryGraphClass()
    {
        writer.Write("public static class ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Graph");

        writer.BeginBlock();

        GenerateCreateGraphMethod();

        writer.EndBlock(); // class
    }

    private void GenerateCreateGraphMethod()
    {
        writer.Write("public static ");
        GeneralEmission.GenerateGenericStoryType(typeof(StoryGraph<,>), settings, writer);
        writer.WriteLine(" CreateStoryGraph()");

        writer.BeginBlock();

        GenerateVertexDictionary();

        writer.WriteLine();

        FlowGraph reverseGraph = flowGraph.Reverse();

        foreach (FlowVertex vertex in flowGraph.Vertices.Values)
        {
            GenerateVertex(vertex, reverseGraph);
        }

        GenerateStartEdges();

        writer.Write("return new ");
        GeneralEmission.GenerateGenericStoryType(typeof(StoryGraph<,>), settings, writer);
        writer.WriteLine("(vertices, startEdges);");

        writer.EndBlock(); // method
    }

    private void GenerateVertexDictionary()
    {
        void GenerateVertexDictionaryType()
        {
            writer.Write("global::");
            writer.Write(typeof(Dictionary<,>).FullName?[..typeof(Dictionary<,>).FullName!.IndexOf('`')]);
            writer.Write("<long, ");
            GeneralEmission.GenerateGenericStoryType(typeof(StoryVertex<,>), settings, writer);
            writer.Write('>');
        }

        GenerateVertexDictionaryType();
        writer.Write(" vertices = new ");
        GenerateVertexDictionaryType();
        writer.Write('(');
        writer.Write(flowGraph.Vertices.Count);
        writer.WriteLine(");");
    }

    private void GenerateVertex(FlowVertex vertex, FlowGraph reverseGraph)
    {
        if (!vertex.IsVisible)
        {
            return;
        }

        writer.BeginBlock();

        IOutputStatementNode outputStatement = vertex.AssociatedStatement switch
        {
            IOutputStatementNode os => os,
            FlowBranchingStatementNode { Original: IOutputStatementNode os } => os,
            _ => throw new InvalidOperationException(),
        };

        GenerateOptions(outputStatement); // includes writer.WriteLine()

        GenerateOutgoingEdges(vertex);
        writer.WriteLine();

        GenerateIncomingEdges(vertex, reverseGraph);
        writer.WriteLine();

        GenerateVertexCreation(vertex, outputStatement);

        writer.EndBlock(); // scope
        writer.WriteLine();
    }

    private void GenerateOptions(IOutputStatementNode outputStatement)
    {
        if (outputStatement is not IOptionsStatementNode optionsStatement)
        {
            return;
        }

        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write("[] options = { ");

        foreach (ExpressionNode optionExpression in optionsStatement.OptionExpressions)
        {
            GeneralEmission.GenerateExpression(optionExpression, writer);
            writer.Write(", ");
        }

        writer.WriteLine("};");
        writer.WriteLine();
    }

    private void GenerateOutgoingEdges(FlowVertex vertex)
    {
        writer.Write("global::");
        writer.Write(typeof(StoryEdge).FullName);
        writer.WriteLine("[] outgoingEdges = ");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (FlowEdge edge in flowGraph.OutgoingEdges[vertex.Index])
        {
            writer.Write("new global::");
            writer.Write(typeof(StoryEdge).FullName);
            writer.Write('(');
            writer.Write(edge.ToVertex);
            writer.Write(", ");
            writer.Write(vertex.Index); // fromVertex
            writer.Write(", ");
            writer.Write(edge.IsWeak ? "true" : "false");
            writer.WriteLine("),");
        }

        writer.Indent--;
        writer.WriteLine("};");
    }

    private void GenerateIncomingEdges(FlowVertex vertex, FlowGraph reverseGraph)
    {
        writer.Write("global::");
        writer.Write(typeof(StoryEdge).FullName);
        writer.Write("[] incomingEdges = ");

        writer.WriteLine();
        writer.WriteLine('{');
        writer.Indent++;

        foreach (FlowEdge edge in flowGraph.StartEdges)
        {
            if (edge.ToVertex == vertex.Index)
            {
                writer.Write("new global::");
                writer.Write(typeof(StoryEdge).FullName);
                writer.Write('(');
                writer.Write(vertex.Index); // toVertex
                writer.Write(", ");
                writer.Write(Constants.StartState); // fromVertex
                writer.Write(", ");
                writer.Write(edge.IsWeak ? "true" : "false");
                writer.WriteLine("),");
            }
        }

        if (reverseGraph.OutgoingEdges.TryGetValue(vertex.Index, out ImmutableList<FlowEdge>? edges))
        {
            foreach (FlowEdge edge in edges)
            {
                writer.Write("new global::");
                writer.Write(typeof(StoryEdge).FullName);
                writer.Write('(');
                writer.Write(vertex.Index); // toVertex
                writer.Write(", ");
                writer.Write(edge.ToVertex); // fromVertex
                writer.Write(", ");
                writer.Write(edge.IsWeak ? "true" : "false");
                writer.WriteLine("),");
            }
        }

        writer.Indent--;
        writer.WriteLine("};");
    }

    private void GenerateVertexCreation(FlowVertex vertex, IOutputStatementNode outputStatement)
    {
        writer.Write("vertices[");
        writer.Write(vertex.Index);
        writer.Write("] = new ");
        GeneralEmission.GenerateGenericStoryType(typeof(StoryVertex<,>), settings, writer);
        writer.Write('(');
        writer.Write(vertex.Index);
        writer.Write(", ");
        GeneralEmission.GenerateExpression(outputStatement.OutputExpression, writer);
        writer.Write(", ");

        void GenerateOptionType() => GeneralEmission.GenerateType(settings.OptionType, writer);

        void WrapInReadOnlyList(Action listGenerator, Action typeGenerator)
        {
            writer.Write("new global::");
            writer.Write(typeof(ReadOnlyList<>).FullName?[..typeof(ReadOnlyList<>).FullName!.IndexOf('`')]);
            writer.Write('<');
            typeGenerator();
            writer.Write(">(");
            listGenerator();
            writer.Write(')');
        }

        if (outputStatement is IOptionsStatementNode)
        {
            WrapInReadOnlyList(() => writer.Write("options"), GenerateOptionType);
        }
        else
        {
            WrapInReadOnlyList(() =>
            {
                writer.Write("global::");
                writer.Write(typeof(Array).FullName);
                writer.Write(".Empty<");
                GeneralEmission.GenerateType(settings.OptionType, writer);
                writer.Write(">()");
            }, GenerateOptionType);
        }

        writer.Write(", ");
        WrapInReadOnlyList(() => writer.Write("outgoingEdges"), () => writer.Write(typeof(StoryEdge).FullName));

        writer.Write(", ");
        WrapInReadOnlyList(() => writer.Write("incomingEdges"), () => writer.Write(typeof(StoryEdge).FullName));

        writer.WriteLine(");");
    }

    private void GenerateStartEdges()
    {
        writer.Write("global::");
        writer.Write(typeof(StoryEdge).FullName);
        writer.Write("[] startEdges = new global::");
        writer.Write(typeof(StoryEdge).FullName);
        writer.Write('[');
        writer.Write(flowGraph.StartEdges.Length);
        writer.WriteLine("];");

        int i = 0;

        foreach (FlowEdge edge in flowGraph.StartEdges)
        {
            if (!edge.IsStory)
            {
                continue;
            }

            writer.Write("startEdges[");
            writer.Write(i);
            writer.Write("] = new global::");
            writer.Write(typeof(StoryEdge).FullName);
            writer.Write('(');
            writer.Write(edge.ToVertex);
            writer.Write(", ");
            writer.Write(Constants.StartState);
            writer.Write(", ");
            writer.Write(edge.IsWeak ? "true" : "false");
            writer.WriteLine(");");

            i++;
        }
    }
}
