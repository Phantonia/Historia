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
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public static class GeneralEmission
{
    public static void GenerateClassHeader(string stateMachineOrSnapshot, Settings settings, IndentedTextWriter writer)
    {
        writer.Write("public sealed class ");
        writer.Write(settings.StoryName);
        writer.Write(stateMachineOrSnapshot);
        writer.Write(" : global::Phantonia.Historia.IStory");
        writer.Write(stateMachineOrSnapshot);
        writer.Write('<');
        GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GenerateType(settings.OptionType, writer);
        writer.WriteLine('>');
    }

    public static void GenerateFields(Settings settings, bool readOnly, bool publicFields, IndentedTextWriter writer)
    {
        writer.Write("private ");

        if (readOnly)
        {
            writer.Write("readonly ");
        }

        writer.WriteLine("int optionsCount;");

        writer.Write("private readonly ");
        GenerateType(settings.OptionType, writer);
        writer.WriteLine("[] options;");

        if (publicFields)
        {
            writer.Write("internal ");
        }
        else
        {
            writer.Write("private ");
        }

        if (readOnly)
        {
            writer.Write("readonly ");
        }

        writer.WriteLine("Fields fields;");
    }

    public static void GenerateProperties(Settings settings, bool readOnly, IndentedTextWriter writer)
    {
        writer.Write("public bool NotStartedStory { get; ");

        if (!readOnly)
        {
            writer.Write("private set; ");
        }

        writer.WriteLine("} = true;");

        writer.WriteLine();

        writer.Write("public bool FinishedStory { get; ");

        if (!readOnly)
        {
            writer.Write("private set; ");
        }

        writer.WriteLine("} = false;");

        writer.WriteLine();

        writer.Write("public global::Phantonia.Historia.ReadOnlyList<");
        GenerateType(settings.OptionType, writer);
        writer.WriteLine("> Options");
        writer.BeginBlock();
        writer.WriteLine("get");
        writer.BeginBlock();

        writer.Write("return new global::Phantonia.Historia.ReadOnlyList<");
        GenerateType(settings.OptionType, writer);
        writer.WriteLine(">(options, 0, optionsCount);");

        writer.EndBlock(); // get
        writer.EndBlock(); // property

        writer.WriteLine();

        writer.Write("public ");
        GenerateType(settings.OutputType, writer);
        writer.Write(" Output { get; ");

        if (!readOnly)
        {
            writer.Write("private set; ");
        }

        writer.WriteLine('}');
    }

    public static void GeneratePublicOutcomes(SymbolTable symbolTable, bool readOnly, IndentedTextWriter writer)
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

            writer.WriteLine();
            writer.BeginBlock();

            writer.WriteLine("get");
            writer.BeginBlock();

            if (outcome is SpectrumSymbol spectrum)
            {
                writer.Write("int value = fields.");
                GenerateSpectrumPositiveFieldName(spectrum, writer);
                writer.Write(" * ");
                writer.Write(spectrum.Intervals.First().Value.UpperDenominator);
                writer.WriteLine(';');
                writer.WriteLine();

                for (int i = 0; i < spectrum.OptionNames.Length - 1; i++)
                {
                    string optionName = spectrum.OptionNames[i];

                    SpectrumInterval interval = spectrum.Intervals[optionName];

                    writer.Write("if (value <");

                    if (interval.Inclusive)
                    {
                        writer.Write('=');
                    }

                    writer.Write(" fields.");
                    GenerateSpectrumTotalFieldName(spectrum, writer);
                    writer.Write(" * ");
                    writer.Write(interval.UpperNumerator);
                    writer.WriteLine(')');
                    writer.BeginBlock();

                    writer.Write("return Spectrum");
                    writer.Write(spectrum.Name);
                    writer.Write('.');
                    writer.Write(optionName);
                    writer.WriteLine(';');

                    writer.EndBlock(); // if/else if

                    writer.Write("else ");
                }

                writer.WriteLine();
                writer.BeginBlock();

                writer.Write("return Spectrum");
                writer.Write(spectrum.Name);
                writer.Write('.');
                writer.Write(spectrum.OptionNames[^1]);
                writer.WriteLine(';');

                writer.EndBlock(); // else
            }
            else
            {
                writer.Write("return (Outcome");
                writer.Write(outcome.Name);
                writer.Write(")fields.");
                GenerateOutcomeFieldName(outcome, writer);
                writer.WriteLine(';');
            }

            writer.EndBlock(); // get

            writer.EndBlock(); // property

            writer.WriteLine();

            if (symbol is SpectrumSymbol spectrum2) // name spectrum already exists up there
            {
                writer.Write("public double Value");
                writer.WriteLine(outcome.Name);
                writer.BeginBlock();

                writer.WriteLine("get");
                writer.BeginBlock();

                writer.Write("return (double)fields.");
                GenerateSpectrumPositiveFieldName(spectrum2, writer);
                writer.Write(" / (double)fields.");
                GenerateSpectrumTotalFieldName(spectrum2, writer);
                writer.Write(';');

                writer.EndBlock(); // get

                writer.EndBlock(); // Value property

                writer.WriteLine();
            }
        }
    }

    public static void GenerateExplicitInterfaceImplementations(string stateMachineOrSnapshot, Settings settings, IndentedTextWriter writer)
    {
        writer.Write("object global::Phantonia.Historia.IStory");
        writer.Write(stateMachineOrSnapshot);
        writer.WriteLine(".Output");
        writer.WriteManyLines(
            """
            {
                get
                {
                    return Output;
                }
            }
            """);
        writer.WriteLine();

        writer.Write("global::System.Collections.Generic.IReadOnlyList<");
        GenerateType(settings.OptionType, writer);
        writer.Write("> global::Phantonia.Historia.IStory");
        writer.Write(stateMachineOrSnapshot);
        writer.Write('<');
        GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GenerateType(settings.OptionType, writer);
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

        writer.Write("global::System.Collections.Generic.IReadOnlyList<object?> global::Phantonia.Historia.IStory");
        writer.Write(stateMachineOrSnapshot);
        writer.WriteLine(".Options");
        writer.BeginBlock();
        writer.WriteLine("get");
        writer.BeginBlock();
        writer.Write("return new global::Phantonia.Historia.ObjectReadOnlyList<");
        GenerateType(settings.OptionType, writer);
        writer.WriteLine(">(Options);");
        writer.EndBlock(); // get
        writer.EndBlock(); // Options property
    }

    public static void GenerateType(TypeSymbol type, IndentedTextWriter writer)
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

    public static void GenerateGenericStoryType(string typeName, Settings settings, IndentedTextWriter writer)
    {
        writer.Write(typeName);
        writer.Write('<');
        GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GenerateType(settings.OptionType, writer);
        writer.Write('>');
    }

    public static void GenerateGenericStoryType(Type type, Settings settings, IndentedTextWriter writer) => GenerateGenericStoryType($"global::{type.FullName?[..type.FullName.IndexOf('`')]}", settings, writer);

    public static void GenerateExpression(ExpressionNode expression, Settings settings, IndentedTextWriter writer)
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
            }, settings, writer);
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
                    GenerateExpression(argument.Expression, settings, writer);
                    writer.Write(", ");
                }
                GenerateExpression(recordCreation.BoundArguments[^1].Expression, settings, writer);
                writer.Write(')');
                return;
            case BoundEnumOptionExpressionNode enumOptionExpression:
                writer.Write(enumOptionExpression.EnumName);
                writer.Write('.');
                writer.Write(enumOptionExpression.OptionName);
                return;
            case SynthesizedEmptyExpressionNode:
                writer.Write("default(");
                GenerateType(settings.OutputType, writer);
                writer.Write(')');
                return;
        }

        Debug.Assert(false);
    }

    public static void GenerateTrackerFieldName(CallerTrackerSymbol tracker, IndentedTextWriter writer)
    {
        writer.Write("tracker");

        if (tracker.Index >= 0)
        {
            writer.Write(tracker.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-tracker.Index);
        }
    }

    public static string GetLoopSwitchFieldName(LoopSwitchStatementNode loopSwitch) => $"ls{loopSwitch.Index}";

    public static void GenerateOutcomeFieldName(OutcomeSymbol outcome, IndentedTextWriter writer)
    {
        writer.Write("outcome");

        if (outcome.Index >= 0)
        {
            writer.Write(outcome.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-outcome.Index);
        }
    }

    public static void GenerateSpectrumTotalFieldName(SpectrumSymbol spectrum, IndentedTextWriter writer)
    {
        writer.Write("total");

        if (spectrum.Index >= 0)
        {
            writer.Write(spectrum.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-spectrum.Index);
        }
    }

    public static void GenerateSpectrumPositiveFieldName(SpectrumSymbol spectrum, IndentedTextWriter writer)
    {
        writer.Write("positive");

        if (spectrum.Index >= 0)
        {
            writer.Write(spectrum.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-spectrum.Index);
        }
    }

    public static void GenerateLoopSwitchFieldName(LoopSwitchStatementNode loopSwitch, IndentedTextWriter writer)
    {
        writer.Write("ls");
        writer.Write(loopSwitch.Index);
    }

    public static int GetMaximumOptionCount(StoryNode boundStory)
    {
        return boundStory.FlattenHierarchie()
                         .Select(n => n switch
                         {
                             SwitchStatementNode s => s.Options.Length,
                             LoopSwitchStatementNode l => l.Options.Length,
                             _ => int.MinValue
                         })
                         .Append(0) // if sequence is empty, at least have one number
                         .Max();
    }
}
