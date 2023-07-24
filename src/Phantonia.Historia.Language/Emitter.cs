// uncomment this to see the example story class below
// #define EXAMPLE_STORY

using Microsoft.CodeAnalysis.CSharp;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.SemanticAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language;

public sealed class Emitter
{
    public Emitter(StoryNode boundStory, Settings settings, FlowGraph flowGraph)
    {
        this.boundStory = boundStory;
        this.settings = settings;
        this.flowGraph = flowGraph;
    }

    private readonly StoryNode boundStory;
    private readonly Settings settings;
    private readonly FlowGraph flowGraph;

    public string GenerateCSharpText()
    {
        const string className = "HistoriaStory";

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
            """);

        writer.WriteLine();

        writer.Write("public System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.Write("> Options { get; private set; } = System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.WriteLine();

        writer.Write("public ");
        GenerateType(writer, settings.OutputType);
        writer.WriteLine(" Output { get; private set; }");
        writer.WriteLine();

        writer.WriteManyLines(
            $$"""
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

        GenerateGetNextStateMethod(writer);

        writer.WriteLine();

        GenerateGetOutputMethod(writer);

        writer.WriteLine();

        GenerateGetOptionsMethod(writer);

        writer.WriteLine();

        GenerateTypes(writer);
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

    private void GenerateGetOutputMethod(IndentedTextWriter writer)
    {
        writer.Write("private ");
        GenerateType(writer, settings.OutputType);
        writer.WriteLine(" GetOutput()");

        writer.WriteManyLines(
            $$"""
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

    private void GenerateGetOptionsMethod(IndentedTextWriter writer)
    {
        writer.Write("private System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine("> GetOptions()");

        writer.WriteManyLines(
              $$"""
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

                foreach (SwitchOptionNode option in switchStatement.Options)
                {
                    GenerateExpression(writer, option.Expression);
                    writer.Write(", ");
                }

                writer.WriteLine("});");
                writer.Indent--;
            }
        }

        writer.Indent--;

        writer.WriteLine('}');
        writer.WriteLine();

        writer.Write("return System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateTypes(IndentedTextWriter writer)
    {
        foreach (TopLevelNode topLevelNode in boundStory.TopLevelNodes)
        {
            if (topLevelNode is not BoundSymbolDeclarationNode
                {
                    Declaration: TypeSymbolDeclarationNode declaration,
                    Symbol: TypeSymbol symbol,
                })
            {
                continue;
            }

            switch (symbol)
            {
                case RecordTypeSymbol recordSymbol:
                    GenerateRecordDeclaration(writer, recordSymbol);
                    continue;
                default:
                    Debug.Assert(false);
                    return;
            }
        }
    }

    private static void GenerateRecordDeclaration(IndentedTextWriter writer, RecordTypeSymbol record)
    {
        writer.WriteManyLines(
                        $$"""
                        public readonly struct @{{record.Name}}
                        {
                        """);
        writer.Indent++;

        writer.Write($"internal @{record.Name}(");

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(writer, property.Type);
            writer.Write($" @{property.Name}, ");
        }

        GenerateType(writer, record.Properties[^1].Type);
        writer.WriteLine($" @{record.Properties[^1].Name})");
        writer.WriteLine('{');

        writer.Indent++;

        foreach (PropertySymbol property in record.Properties)
        {
            writer.WriteLine($"this.@{property.Name} = @{property.Name};");
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(writer, property.Type);
            writer.WriteLine($" @{property.Name} {{ get; }}");
            writer.WriteLine();
        }

        GenerateType(writer, record.Properties[^1].Type);
        writer.WriteLine($" @{record.Properties[^1].Name} {{ get; }}");

        writer.Indent--;
        writer.WriteLine('}');
    }

    private static void GenerateExpression(IndentedTextWriter writer, ExpressionNode expression)
    {
        TypedExpressionNode? typedExpression = expression as TypedExpressionNode;
        Debug.Assert(typedExpression is not null);

        switch (typedExpression.Expression)
        {
            case IntegerLiteralExpressionNode { Value: int intValue }:
                writer.Write(intValue);
                return;
            case StringLiteralExpressionNode { StringLiteral: string stringValue }:
                writer.Write(SymbolDisplay.FormatLiteral(stringValue, quote: true));
                return;
            case BoundRecordCreationExpressionNode recordCreation:
                writer.Write("new @");
                writer.Write(recordCreation.Record.Name);
                writer.Write('(');
                foreach (BoundArgumentNode argument in recordCreation.BoundArguments.Take(recordCreation.BoundArguments.Length - 1))
                {
                    GenerateExpression(writer, argument.Expression);
                    writer.Write(", ");
                }
                GenerateExpression(writer, recordCreation.BoundArguments[^1].Expression);
                writer.Write(')');
                return;
        }

        Debug.Assert(false);
    }

    private static void GenerateType(IndentedTextWriter writer, TypeSymbol type)
    {
        switch (type)
        {
            case BuiltinTypeSymbol { Type: BuiltinType.Int }:
                writer.Write("int");
                return;
            case BuiltinTypeSymbol { Type: BuiltinType.String }:
                writer.Write("string");
                return;
            default:
                writer.Write("@" + type.Name);
                return;
        }
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
