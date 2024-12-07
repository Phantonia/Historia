using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class StateMachineEmitter(StoryNode boundStory, FlowGraph flowGraph, Settings settings, SymbolTable symbolTable, IndentedTextWriter writer)
{
    public void GenerateStateMachineClass()
    {
        GeneralEmission.GenerateClassHeader("StateMachine", settings, writer);

        writer.BeginBlock();

        GenerateConstructor();

        writer.WriteLine();

        GeneralEmission.GenerateFields(settings, readOnly: false, publicFields: false, writer);

        writer.WriteLine();

        GeneralEmission.GenerateProperties(settings, readOnly: false, writer);

        writer.WriteLine();

        GeneralEmission.GeneratePublicOutcomes(symbolTable, readOnly: false, writer); // ends in a writer.WriteLine()

        GeneralEmission.GenerateReferences(symbolTable, readOnly: false, writer); // ends in writer.WriteLine()

        GenerateContinueMethods();

        writer.WriteLine();

        GenerateSnapshotMethods();

        writer.WriteLine();

        GenerateRestoreCheckpointMethod();

        writer.WriteLine();

        GenerateExplicitInterfaceImplementations();

        writer.EndBlock(); // class
    }

    private void GenerateConstructor()
    {
        writer.Write("public ");
        writer.Write(settings.StoryName);
        writer.Write("StateMachine(");

        bool first = true;

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            if (symbol is not ReferenceSymbol reference)
            {
                continue;
            }

            if (!first)
            {
                writer.Write(", ");
            }

            writer.Write('I');
            writer.Write(reference.Interface.Name);
            writer.Write(" reference");
            writer.Write(reference.Name);

            first = false;
        }

        writer.WriteLine(')');
        writer.BeginBlock();

        writer.Write("fields.state = ");
        writer.Write(Constants.StartState);
        writer.WriteLine(';');

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            if (symbol is not ReferenceSymbol reference)
            {
                continue;
            }

            writer.Write("fields.reference");
            writer.Write(reference.Name);
            writer.Write(" = reference");
            writer.Write(reference.Name);
            writer.WriteLine(';');
        }

        int maxOptionCount = GeneralEmission.GetMaximumOptionCount(boundStory);

        if (maxOptionCount != 0)
        {
            writer.Write("options = new ");
            GeneralEmission.GenerateType(settings.OptionType, writer);
            writer.Write('[');
            writer.Write(maxOptionCount);
            writer.WriteLine("];");
        }
        else
        {
            writer.Write("options = global::System.Array.Empty<");
            GeneralEmission.GenerateType(settings.OptionType, writer);
            writer.WriteLine(">();");
        }

        writer.EndBlock(); // constructor
    }

    private void GenerateContinueMethods()
    {
        writer.WriteManyLines(
           $$"""
            public bool TryContinue()
            {
                if (FinishedStory || Options.Count != 0)
                {
                    return false;
                }

                Heart.StateTransition(ref fields, 0);
                Output = Heart.GetOutput(ref fields);
                Heart.GetOptions(ref fields, options, ref optionsCount);
            
                if (fields.state != {{Constants.StartState}})
                {
                    NotStartedStory = false;
                }
            
                if (fields.state == {{Constants.EndState}})
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

                Heart.StateTransition(ref fields, option);
                Output = Heart.GetOutput(ref fields);
                Heart.GetOptions(ref fields, options, ref optionsCount);

                if (fields.state != {{Constants.StartState}})
                {
                    NotStartedStory = false;
                }

                if (fields.state == {{Constants.EndState}})
                {
                    FinishedStory = true;
                }

                return true;
            }
            """);
    }

    private void GenerateSnapshotMethods()
    {
        writer.Write("public ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot CreateSnapshot()");
        writer.BeginBlock();
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write("[] optionsCopy = new ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine("[options.Length];");
        writer.WriteLine("global::System.Array.Copy(options, optionsCopy, options.Length);");
        writer.Write("return new ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot(fields, Output, optionsCopy, optionsCount);");
        writer.EndBlock(); // CreateSnapshot method

        writer.WriteLine();

        writer.Write("public void RestoreSnapshot(");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot snapshot)");
        writer.BeginBlock();
        writer.WriteLine("fields = snapshot.fields;");
        writer.WriteLine("Output = Heart.GetOutput(ref fields);");
        writer.WriteLine("Heart.GetOptions(ref fields, options, ref optionsCount);");
        writer.EndBlock(); // RestoreSnapshot method
    }

    private void GenerateRestoreCheckpointMethod()
    {
        if (!flowGraph.Vertices.Any(v => v.Value.IsCheckpoint))
        {
            return;
        }

        writer.Write("public void RestoreCheckpoint(");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint checkpoint)");

        writer.BeginBlock();

        writer.WriteManyLines(
            """
            if (!checkpoint.IsReady())
            {
                throw new global::System.ArgumentException("Checkpoint is not ready, i.e. fully initialized");
            }
            """);
        writer.WriteLine();

        writer.WriteLine("fields.state = checkpoint.Index;");
        writer.WriteLine();

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            if (symbol is not OutcomeSymbol { IsPublic: true } outcome)
            {
                continue;
            }

            if (symbol is SpectrumSymbol spectrum)
            {
                writer.Write("if (checkpoint.Spectrum");
                writer.Write(symbol.Name);
                writer.Write(".Kind == global::Phantonia.Historia.CheckpointOutcomeKind.Required || (checkpoint.Spectrum");
                writer.Write(symbol.Name);
                writer.Write(".Kind == global::Phantonia.Historia.CheckpointOutcomeKind.Optional && checkpoint.Spectrum");
                writer.Write(symbol.Name);
                writer.WriteLine(".TotalCount != 0))");

                writer.BeginBlock();

                writer.Write("fields.");
                GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
                writer.Write(" = checkpoint.Spectrum");
                writer.Write(symbol.Name);
                writer.WriteLine(".PositiveCount;");

                writer.Write("fields.");
                GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
                writer.Write(" = checkpoint.Spectrum");
                writer.Write(symbol.Name);
                writer.WriteLine(".TotalCount;");

                writer.EndBlock();

                writer.WriteLine();

                continue;
            }

            writer.Write("if (checkpoint.Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Kind == global::Phantonia.Historia.CheckpointOutcomeKind.Required || (checkpoint.Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Kind == global::Phantonia.Historia.CheckpointOutcomeKind.Optional && checkpoint.Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Option != Outcome");
            writer.Write(symbol.Name);
            writer.WriteLine(".Unset))");

            writer.BeginBlock();

            writer.Write("fields.");
            GeneralEmission.GenerateOutcomeFieldName(outcome, writer);
            writer.Write(" = (int)checkpoint.Outcome");
            writer.Write(symbol.Name);
            writer.WriteLine(".Option - 1;"); // outcome enums have the additional "Unset" option, so all options are shifted upwards by 1

            writer.EndBlock();

            writer.WriteLine();
        }

        writer.WriteManyLines(
            """
            Output = Heart.GetOutput(ref fields);
            Heart.GetOptions(ref fields, options, ref optionsCount);
            """);

        writer.EndBlock();
    }

    private void GenerateExplicitInterfaceImplementations()
    {
        GeneralEmission.GenerateExplicitInterfaceImplementations("StateMachine", settings, writer);

        writer.WriteLine("global::Phantonia.Historia.IStorySnapshot global::Phantonia.Historia.IStoryStateMachine.CreateSnapshot()");
        writer.BeginBlock();
        writer.WriteLine("return CreateSnapshot();");
        writer.EndBlock(); // CreateSnapshot method

        writer.WriteLine();

        writer.Write("global::Phantonia.Historia.IStorySnapshot<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write("> global::Phantonia.Historia.IStoryStateMachine<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine(">.CreateSnapshot()");
        writer.BeginBlock();
        writer.WriteLine("return CreateSnapshot();");
        writer.EndBlock(); // CreateSnapshot method
    }
}
