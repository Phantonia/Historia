// uncomment this to see the example story class below
// #define EXAMPLE_STORY

using Phantonia.Historia.Language.Ast.Expressions;
using Phantonia.Historia.Language.Flow;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

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

        StringBuilder bob = new();

        bob.AppendLine($$"""
                         public sealed class {{className}}
                         {
                             public {{className}}()
                             {
                         
                             }
                      
                             private int state;
                      
                             public {{outputType}} Output { get; private set; }

                             public bool TryContinue()
                             {
                                 state = GetNextState(0);
                                 Output = GetOutput();
                                 return true;
                             }

                         """);


        GenerateGetNextStateMethod(bob);

        bob.AppendLine();

        GenerateGetOutputMethod(bob, outputType);

        bob.AppendLine("}");

        return bob.ToString();
    }

    private void GenerateGetNextStateMethod(StringBuilder bob)
    {
        const string Tab = "    ";

        bob.AppendLine("""
                           private int GetNextState(int option)
                           {
                               switch (state, option)
                               {
                       """);

        // currently we only have linear states
        foreach ((int index, ImmutableList<int> edges) in flowGraph.OutgoingEdges)
        {
            Debug.Assert(edges.Count == 1);

            bob.AppendLine($"{Tab}{Tab}{Tab}case ({index}, _):");
            bob.AppendLine($"{Tab}{Tab}{Tab}{Tab}return {edges[0]};");
        }

        bob.AppendLine("""
                               }

                               throw new System.InvalidOperationException("Invalid state");
                           }
                       """);

    }

    private void GenerateGetOutputMethod(StringBuilder bob, string outputType)
    {
        const string Tab = "    ";

        bob.AppendLine($$"""
                             private {{outputType}} GetOutput()
                             {
                                 switch (state)
                                 {
                         """);

        // currently we only have linear states
        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            if (vertex.OutputExpression is null)
            {
                continue;
            }

            bob.AppendLine($"{Tab}{Tab}{Tab}case {index}:");
            bob.Append($"{Tab}{Tab}{Tab}{Tab}return ");
            GenerateExpression(bob, vertex.OutputExpression);
            bob.AppendLine(";");
        }

        bob.AppendLine("""
                               }

                               throw new System.InvalidOperationException("Invalid state");
                           }
                       """);
    }

    private static void GenerateExpression(StringBuilder bob, ExpressionNode expression)
    {
        switch (expression)
        {
            case IntegerLiteralExpressionNode { Value: int intValue }:
                bob.Append(intValue);
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
