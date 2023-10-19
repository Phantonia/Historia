using Microsoft.CodeAnalysis.CSharp;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private const int EndState = FlowGraph.FinalVertex;
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

        if (settings.Namespace != "")
        {
            writer.Write("namespace ");
            writer.WriteLine(settings.Namespace);
            writer.Write('{');
            writer.Indent++;
        }

        GenerateStoryClass();

        writer.WriteLine();

        GenerateTypeDeclarations();

        if (settings.Namespace != "")
        {
            writer.Indent--;
            writer.WriteLine('}');
        }

        Debug.Assert(writer.Indent == 0);
    }

    private void GenerateStoryClass()
    {
        writer.Write("public sealed class @");
        writer.Write(settings.StoryName);
        writer.Write(" : global::Phantonia.Historia.IStory<");
        GenerateType(settings.OutputType);
        writer.Write(", ");
        GenerateType(settings.OptionType);
        writer.WriteLine('>');
        writer.WriteLine('{');

        writer.Indent++;

        writer.Write("public @");
        writer.Write(settings.StoryName);
        writer.WriteLine("()");
        writer.WriteLine('{');

        writer.Indent++;

        writer.Write("state = ");
        writer.Write(StartState);
        writer.WriteLine(';');

        int maxOptionCount = GetMaximumOptionCount();

        if (maxOptionCount != 0)
        {
            writer.Write("options = new ");
            GenerateType(settings.OptionType);
            writer.Write('[');
            writer.Write(maxOptionCount);
            writer.WriteLine("];");
        }
        else
        {
            writer.Write("options = global::System.Array.Empty<");
            GenerateType(settings.OptionType);
            writer.WriteLine(">();");
        }

        writer.Indent--;

        writer.WriteLine('}');
        writer.WriteLine();

        writer.WriteLine("private int state;");
        writer.WriteLine("private int optionsCount;");

        writer.Write("private ");
        GenerateType(settings.OptionType);
        writer.WriteLine("[] options;");

        GenerateFields();

        writer.WriteLine();

        writer.WriteLine("public bool NotStartedStory { get; private set; } = true;");

        writer.WriteLine();

        writer.WriteLine("public bool FinishedStory { get; private set; } = false;");

        writer.WriteLine();

        writer.Write("public global::Phantonia.Historia.ReadOnlyList<");
        GenerateType(settings.OptionType);
        writer.WriteLine("> Options");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("get");
        writer.WriteLine('{');
        writer.Indent++;

        writer.Write("return new global::Phantonia.Historia.ReadOnlyList<");
        GenerateType(settings.OptionType);
        writer.WriteLine(">(options, 0, optionsCount);");

        writer.Indent--;
        writer.WriteLine('}');
        writer.Indent--;
        writer.WriteLine('}');


        writer.WriteLine();

        writer.Write("public ");
        GenerateType(settings.OutputType);
        writer.WriteLine(" Output { get; private set; }");
        writer.WriteLine();

        GeneratePublicOutcomes();

        writer.WriteManyLines(
            $$"""
            public bool TryContinue()
            {
                if (FinishedStory || Options.Count != 0)
                {
                    return false;
                }

                StateTransition(0);
                Output = GetOutput();
                GetOptions();
            
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
                if (FinishedStory || option < 0 || option >= Options.Count)
                {
                    return false;
                }

                StateTransition(option);
                Output = GetOutput();
                GetOptions();

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

        writer.WriteManyLines(
            """
            object global::Phantonia.Historia.IStory.Output
            {
                get
                {
                    return Output;
                }
            }
            """);
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

        writer.WriteLine();

        writer.WriteLine("global::System.Collections.Generic.IReadOnlyList<object?> global::Phantonia.Historia.IStory.Options");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("get");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return new global::Phantonia.Historia.ObjectReadOnlyList<");
        GenerateType(settings.OptionType);
        writer.WriteLine(">(Options);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.Indent--;
        writer.WriteLine('}');

        writer.Indent--;
        writer.WriteLine('}');
    }

    private int GetMaximumOptionCount()
    {
        return boundStory.FlattenHierarchie()
                         .Select(n => n switch { SwitchStatementNode s => s.Options.Length, LoopSwitchStatementNode l => l.Options.Length, _ => int.MinValue })
                         .Append(0) // if sequence is empty, at least have one number
                         .Max();
    }

    private void GeneratePublicOutcomes()
    {
        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            if (symbol is not OutcomeSymbol { Public: true } outcome)
            {
                continue;
            }

            void WriteName()
            {
                if (outcome is SpectrumSymbol)
                {
                    writer.Write("Spectrum");
                }
                else
                {
                    writer.Write("Outcome");
                }

                writer.Write(outcome.Name);
            }

            writer.Write("public ");

            WriteName(); // type
            writer.Write(' ');

            WriteName(); // property

            writer.WriteLine(" { get; private set; }");

            writer.WriteLine();

            if (symbol is SpectrumSymbol)
            {
                writer.Write("public double Value");
                writer.Write(outcome.Name);
                writer.WriteLine(" { get; private set; }");
                writer.WriteLine();
            }
        }
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
            case BoundEnumOptionExpressionNode enumOptionExpression:
                writer.Write(enumOptionExpression.EnumName);
                writer.Write('.');
                writer.Write(enumOptionExpression.OptionName);
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
                writer.Write(type.Name);
                return;
        }
    }
}
