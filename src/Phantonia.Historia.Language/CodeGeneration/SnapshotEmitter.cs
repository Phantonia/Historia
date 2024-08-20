using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class SnapshotEmitter(StoryNode boundStory, Settings settings, SymbolTable symbolTable, IndentedTextWriter writer)
{
    public void GenerateSnapshotClass()
    {
        GeneralEmission.GenerateClassHeader("Snapshot", settings, writer);

        writer.BeginBlock();

        GenerateConstructors();

        writer.WriteLine();

        GeneralEmission.GenerateFields(settings, readOnly: true, publicFields: true, writer);

        writer.WriteLine();

        GeneralEmission.GenerateProperties(settings, readOnly: true, writer);

        writer.WriteLine();

        GeneralEmission.GeneratePublicOutcomes(symbolTable, readOnly: true, writer); // ends in writer.WriteLine()

        GenerateContinueMethods();

        writer.WriteLine();

        GenerateExplicitInterfaceImplementations();

        writer.EndBlock(); // class
    }

    private void GenerateConstructors()
    {
        writer.Write("internal ");
        writer.Write(settings.StoryName);
        writer.Write("Snapshot(Fields fields, ");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(" output, ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine("[] options, int optionsCount)");
        writer.BeginBlock();

        writer.WriteLine("this.fields = fields;");
        writer.WriteLine("Output = output;");
        writer.WriteLine("this.options = options;");
        writer.WriteLine("this.optionsCount = optionsCount;");

        writer.Write("NotStartedStory = fields.state == ");
        writer.Write(Constants.StartState);
        writer.WriteLine(';');

        writer.Write("FinishedStory = fields.state == ");
        writer.Write(Constants.EndState);
        writer.WriteLine(';');

        writer.EndBlock(); // constructor
    }

    private void GenerateContinueMethods()
    {
        writer.Write("public ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot? TryContinue()");
        writer.BeginBlock();

        writer.WriteManyLines(
            """
            if (FinishedStory || Options.Count != 0)
            {
                return null;
            }
            """);

        writer.WriteLine();

        GenerateTransition("0");

        writer.EndBlock(); // TryContinue method

        writer.WriteLine();

        writer.Write("public ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot? TryContinueWithOption(int option)");
        writer.BeginBlock();

        writer.WriteManyLines(
            """
            if (FinishedStory || option < 0 || option >= Options.Count)
            {
                return null;
            }
            """);

        GenerateTransition("option");

        writer.EndBlock(); // TryContinueWithOption method
    }

    private void GenerateTransition(string option)
    {
        writer.WriteLine("Fields fieldsCopy = fields;");
        writer.Write("Heart.StateTransition(ref fieldsCopy, ");
        writer.Write(option);
        writer.Write(");");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.WriteLine(" output = Heart.GetOutput(ref fieldsCopy);");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write("[] optionsCopy = new ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine("[options.Length];");
        writer.WriteLine("int optionsCountCopy = optionsCount;");
        writer.WriteLine("Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);");

        writer.Write("return new ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Snapshot(fieldsCopy, output, optionsCopy, optionsCountCopy);");
    }

    private void GenerateExplicitInterfaceImplementations()
    {
        GeneralEmission.GenerateExplicitInterfaceImplementations("Snapshot", settings, writer);

        writer.WriteLine();

        writer.Write("global::Phantonia.Historia.IStorySnapshot<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write(">? global::Phantonia.Historia.IStorySnapshot<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine(">.TryContinue()");
        writer.BeginBlock();
        writer.WriteLine("return TryContinue();");
        writer.EndBlock(); // TryContinue method

        writer.WriteLine();

        writer.Write("global::Phantonia.Historia.IStorySnapshot<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write(">? global::Phantonia.Historia.IStorySnapshot<");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.Write(", ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine(">.TryContinueWithOption(int option)");
        writer.BeginBlock();
        writer.WriteLine("return TryContinueWithOption(option);");
        writer.EndBlock(); // TryContinueWithOption method

        writer.WriteLine();

        writer.WriteLine("global::Phantonia.Historia.IStorySnapshot? global::Phantonia.Historia.IStorySnapshot.TryContinueWithOption(int option)");
        writer.BeginBlock();
        writer.WriteLine("return TryContinueWithOption(option);");
        writer.EndBlock(); // TryContinueWithOption method

        writer.WriteLine("global::Phantonia.Historia.IStorySnapshot? global::Phantonia.Historia.IStorySnapshot.TryContinue()");
        writer.BeginBlock();
        writer.WriteLine("return TryContinue();");
        writer.EndBlock(); // TryContinue method
    }
}
