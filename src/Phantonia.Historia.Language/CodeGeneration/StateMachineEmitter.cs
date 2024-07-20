using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class StateMachineEmitter
{
    public StateMachineEmitter(StoryNode boundStory, Settings settings, SymbolTable symbolTable, IndentedTextWriter writer)
    {
        this.boundStory = boundStory;
        this.settings = settings;
        this.symbolTable = symbolTable;
        this.writer = writer;
    }

    private readonly StoryNode boundStory;
    private readonly Settings settings;
    private readonly SymbolTable symbolTable;
    private readonly IndentedTextWriter writer;

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

        GenerateContinueMethods();

        writer.WriteLine();

        GenerateSnapshotMethods();

        writer.WriteLine();

        GenerateExplicitInterfaceImplementations();

        writer.EndBlock(); // class
    }

    private void GenerateConstructor()
    {
        writer.Write("public ");
        writer.Write(settings.StoryName);
        writer.WriteLine("StateMachine()");
        writer.BeginBlock();

        writer.Write("fields.state = ");
        writer.Write(Constants.StartState);
        writer.WriteLine(';');

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
