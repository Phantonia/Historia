using Microsoft.CodeAnalysis.CSharp;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private const int EndState = FlowGraph.EmptyVertex;
    private const int StartState = -2;

    public Emitter(StoryNode boundStory, Settings settings, FlowGraph flowGraph, SymbolTable symbolTable, TextWriter outputWriter)
    {
        this.boundStory = boundStory;
        this.settings = settings;
        this.flowGraph = flowGraph;
        this.symbolTable = symbolTable;
        writer = new IndentedTextWriter(outputWriter);
    }

    private readonly StoryNode boundStory;
    private readonly Settings settings;
    private readonly FlowGraph flowGraph;
    private readonly SymbolTable symbolTable;
    private readonly IndentedTextWriter writer;

    public void GenerateOutputCode()
    {
        writer.WriteLine("#nullable enable");
        writer.Write("public sealed class @");
        writer.Write(settings.ClassName);
        writer.Write(" : global::Phantonia.Historia.IStory<");
        GenerateType(settings.OutputType);
        writer.Write(", ");
        GenerateType(settings.OptionType);
        writer.WriteLine('>');
        writer.WriteLine('{');

        writer.Indent++;

        writer.Write("public @");
        writer.Write(settings.ClassName);
        writer.WriteLine("()");
        writer.WriteLine('{');

        writer.Indent++;

        writer.Write("state = ");
        writer.Write(StartState);
        writer.WriteLine(';');

        writer.Indent--;

        writer.WriteLine('}');
        writer.WriteLine();

        writer.WriteLine("private int state;");

        GenerateOutcomeFields();

        writer.WriteLine();

        writer.WriteLine("public bool NotStartedStory { get; private set; } = true;");

        writer.WriteLine();

        writer.WriteLine("public bool FinishedStory { get; private set; } = false;");

        writer.WriteLine();

        writer.Write("public global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(settings.OptionType);
        writer.Write("> Options { get; private set; } = global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.WriteLine();

        writer.Write("public ");
        GenerateType(settings.OutputType);
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

                StateTransition(0);
                Output = GetOutput();
                Options = GetOptions();
            
                if (state != {{StartState}})
                {
                    NotStartedStory = false;
                }
            
                if (state == {{EndState}})
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

                StateTransition(option);
                Output = GetOutput();
                Options = GetOptions();

                if (state != {{StartState}})
                {
                    NotStartedStory = false;
                }

                if (state == {{EndState}})
                {
                    FinishedStory = true;
                }

                return true;
            }
            """);

        writer.WriteLine();

        GenerateStateTransitionMethod();

        writer.WriteLine();

        GenerateGetOutputMethod();

        writer.WriteLine();

        GenerateGetOptionsMethod();

        writer.WriteLine();

        GenerateTypeDeclarations();

        writer.WriteLine();
        writer.Write("global::System.Collections.Generic.IReadOnlyList<");
        GenerateType(settings.OptionType);
        writer.Write("> global::Phantonia.Historia.IStory<");
        GenerateType(settings.OutputType);
        writer.Write(", ");
        GenerateType(settings.OptionType);
        writer.WriteLine(">.Options");
        writer.WriteManyLines(
            """
            {
                get
                {
                    return Options;
                }
            }
            """);

        writer.Indent--;

        writer.WriteLine("}");

        Debug.Assert(writer.Indent == 0);
    }

    private void GenerateExpression(ExpressionNode expression)
    {
        TypedExpressionNode? typedExpression = expression as TypedExpressionNode;
        Debug.Assert(typedExpression is not null);

        if (typedExpression.SourceType != typedExpression.TargetType)
        {
            Debug.Assert(typedExpression.TargetType is UnionTypeSymbol);

            writer.Write("new @");
            writer.Write(typedExpression.TargetType.Name);
            writer.Write('(');
            GenerateExpression(typedExpression with
            {
                TargetType = typedExpression.SourceType,
            });
            writer.Write(')');
            return;
        }

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
                    GenerateExpression(argument.Expression);
                    writer.Write(", ");
                }
                GenerateExpression(recordCreation.BoundArguments[^1].Expression);
                writer.Write(')');
                return;
        }

        Debug.Assert(false);
    }

    private void GenerateType(TypeSymbol type)
    {
        switch (type)
        {
            case BuiltinTypeSymbol { Type: BuiltinType.Int }:
                writer.Write("int");
                return;
            case BuiltinTypeSymbol { Type: BuiltinType.String }:
                writer.Write("string?");
                return;
            default:
                writer.Write('@');
                writer.Write(settings.ClassName);
                writer.Write(".@");
                writer.Write(type.Name);
                return;
        }
    }
}
