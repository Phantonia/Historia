// uncomment this to see the example story class below
#define EXAMPLE_STORY

using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language;

public sealed class Emitter
{
    public Emitter(FlowGraph flowGraph)
    {
        this.flowGraph = flowGraph;
    }

    private readonly FlowGraph flowGraph;

    public string GenerateCSharpText()
    {
        const string className = "HistoriaStory";
        const string outputType = "int";
        const string optionsType = "int";

        IndentedTextWriter writer = new(new StringWriter());

        writer.WriteManyLines(
          $$"""
            public sealed class {{className}}
            {
                public {{className}}()
                {
            """);

        writer.Indent += 2;

        writer.Write("Output = ");

        if (flowGraph.Vertices[flowGraph.StartVertex].AssociatedStatement is IOutputStatementNode outputStatement)
        {
            GenerateExpression(writer, outputStatement.OutputExpression);
        }
        else
        {
            writer.Write("default");
        }

        writer.WriteLine(";");

        writer.Indent--;

        writer.WriteManyLines(
          $$"""
            }
                      
            private int state = {{flowGraph.StartVertex}};

            public bool FinishedStory { get; private set; } = false;
            
            public System.Collections.Immutable.ImmutableArray<{{optionsType}}> Options { get; private set; } = System.Collections.Immutable.ImmutableArray<{{optionsType}}>.Empty;
                      
            public {{outputType}} Output { get; private set; }

            public bool TryContinue()
            {
                if (FinishedStory || Options.Length != 0)
                {
                    return false;
                }

                state = GetNextState(0);
                Output = GetOutput();
                Options = GetOptions();
            
                if (state == -1)
                {
                    FinishedStory = true;
                }
            
                return true;
            }

            public bool TryContinueWithOption(int option)
            {
                if (FinishedStory || option < 0 || option >= Options.Length)
                {
                    return false;
                }

                state = GetNextState(option);
                Output = GetOutput();
                Options = GetOptions();

                if (state == -1)
                {
                    FinishedStory = true;
                }

                return true;
            }

            """);

        writer.Indent++;
        GenerateGetNextStateMethod(writer);

        writer.WriteLine();

        GenerateGetOutputMethod(writer, outputType);

        writer.WriteLine();

        GenerateGetOptionsMethod(writer, optionsType);
        writer.Indent--;

        writer.WriteLine("}");

        return ((StringWriter)writer.InnerWriter).ToString();
    }

    private void GenerateGetNextStateMethod(IndentedTextWriter writer)
    {
        writer.WriteManyLines(
            """
            private int GetNextState(int option)
            {
                switch (state, option)
                {
            """);

        writer.Indent += 2;

        foreach ((int index, ImmutableList<int> edges) in flowGraph.OutgoingEdges)
        {
            if (edges.Count == 1)
            {
                writer.WriteLine($"case ({index}, _):");

                writer.Indent++;
                writer.WriteLine($"return {edges[0]};");
                writer.Indent--;
            }
            else if (flowGraph.Vertices[index].AssociatedStatement is SwitchStatementNode switchStatement)
            {
                Debug.Assert(switchStatement.Options.Length == edges.Count);

                for (int i = 0; i < switchStatement.Options.Length; i++)
                {
                    // we know that for each option its index equals that of the associated next vertex
                    // we also know that the order that the options appear in the switch statement node is exactly how each one will be indexed
                    // plus we know that the outgoing edges are in exactly the right order

                    // here we assert that the index of the vertex equals the index of the first statement of the option
                    // however we ignore the case where the option's body is empty - there it would be impossible to get the next statement
                    Debug.Assert(switchStatement.Options[i].Body.Statements.Length == 0 || switchStatement.Options[i].Body.Statements[0].Index == edges[i]);

                    writer.WriteLine($"case ({index}, {i}):");

                    writer.Indent++;
                    writer.WriteLine($"return {edges[i]};");
                    writer.Indent--;
                }
            }
        }

        writer.Indent -= 2;

        writer.WriteManyLines(
            """
                }

                throw new System.InvalidOperationException("Invalid state");
            }
            """);

    }

    private void GenerateGetOutputMethod(IndentedTextWriter writer, string outputType)
    {
        writer.WriteManyLines(
            $$"""
              private {{outputType}} GetOutput()
              {
                  switch (state)
                  {
              """);

        writer.Indent += 2;

        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            ExpressionNode outputExpression;

            switch (vertex.AssociatedStatement)
            {
                case OutputStatementNode outputStatement:
                    outputExpression = outputStatement.OutputExpression;
                    break;
                case SwitchStatementNode switchStatement:
                    outputExpression = switchStatement.OutputExpression;
                    break;
                default:
                    continue;
            }

            if (vertex.AssociatedStatement is not (OutputStatementNode or SwitchStatementNode))
            {
                continue;
            }

            writer.WriteLine($"case {index}:");

            writer.Indent++;
            writer.Write($"return ");
            GenerateExpression(writer, outputExpression);
            writer.WriteLine(";");
            writer.Indent--;
        }

        writer.Indent -= 2;

        writer.WriteManyLines(
            """
                    case -1:
                        return default;
                }

                throw new System.InvalidOperationException("Invalid state");
            }
            """);
    }

    private void GenerateGetOptionsMethod(IndentedTextWriter writer, string optionsType)
    {
        writer.WriteManyLines(
            $$"""
              private System.Collections.Immutable.ImmutableArray<{{optionsType}}> GetOptions()
              {
                  switch (state)
                  {
              """);

        writer.Indent += 2;

        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            if (vertex.AssociatedStatement is SwitchStatementNode switchStatement)
            {
                writer.WriteLine($"case {index}:");

                writer.Indent++;
                writer.Write("return System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { ");

                foreach (OptionNode option in switchStatement.Options)
                {
                    GenerateExpression(writer, option.Expression);
                    writer.Write(", ");
                }

                writer.WriteLine("});");
                writer.Indent--;
            }
        }

        writer.Indent -= 2;

        writer.WriteManyLines(
          $$"""
                }

                return System.Collections.Immutable.ImmutableArray<{{optionsType}}>.Empty;
            }
            """);
    }

    private static void GenerateExpression(IndentedTextWriter writer, ExpressionNode expression)
    {
        switch (expression)
        {
            case IntegerLiteralExpressionNode { Value: int intValue }:
                writer.Write(intValue);
                return;
        }

        Debug.Assert(false);
    }
}

#if EXAMPLE_STORY
public sealed class Story
{
    public readonly record struct OutputType(string Text);
    public readonly record struct SwitchType(ImmutableArray<OptionType> Options);
    public readonly record struct OptionType(string Text);

    public enum StoryStateKind
    {
        None = 0,
        Linear,
        Switch,
    }

    public Story()
    {
        state = 37191;
        StateKind = StoryStateKind.Linear;
    }

    private int state;

    int? somethingHappened = 1;

    public OutputType? Output { get; private set; }

    public SwitchType? SwitchOutput { get; private set; }

    public StoryStateKind StateKind { get; private set; }

    public bool TryContinue()
    {
        if (StateKind != StoryStateKind.Linear)
        {
            return false;
        }

        state = GetNextState(0);

        return true;
    }

    public bool TryContinueWithOption(int option)
    {
        if (StateKind != StoryStateKind.Switch)
        {
            return false;
        }

        state = GetNextState(option);

        return true;
    }

    private int GetNextState(int option)
    {
        while (true)
        {
            switch (state, option)
            {
                case (1249, _):
                    return 7292;
                case (618, 0):
                    return 1829;
                case (618, 1):
                    return 181010;
                case (1739, _):
                    state = somethingHappened == 0 ? 1910 : 19201;
                    continue;
                default: throw new System.InvalidOperationException();
            }
        }
    }

    private OutputType? GetOutput() => throw new System.NotImplementedException();

    private SwitchType? GetSwitchOutput() => throw new System.NotImplementedException();
}
#endif
