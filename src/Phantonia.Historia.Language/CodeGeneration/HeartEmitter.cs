using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class HeartEmitter(FlowGraph flowGraph, StoryNode boundStory, Settings settings, IndentedTextWriter writer)
{
    public void GenerateHeartClass()
    {
        writer.WriteLine("internal static class Heart");
        writer.BeginBlock();

        GenerateOptionsPool(settings, writer);
        writer.WriteLine();

        StateTransitionEmitter stateTransitionEmitter = new(flowGraph, settings, writer);
        stateTransitionEmitter.GenerateStateTransitionMethod();

        writer.WriteLine();

        OutputEmitter outputEmitter = new(flowGraph, settings, writer);
        outputEmitter.GenerateOutputMethods();

        writer.EndBlock();
    }

    private void GenerateOptionsPool(Settings settings, IndentedTextWriter writer)
    {
        void GenerateThreadLocalType()
        {
            writer.Write("global::");
            writer.Write(nameof(System));
            writer.Write('.');
            writer.Write(nameof(System.Threading));
            writer.Write('.');
            writer.Write(nameof(System.Threading.ThreadLocal<int>));
            writer.Write('<');
            GeneralEmission.GenerateType(settings.OptionType, writer);
            writer.Write("[]>");
        }

        writer.Write("private static readonly ");
        GenerateThreadLocalType();
        writer.Write(" optionsPool = new ");
        GenerateThreadLocalType();
        writer.Write("(() => new ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write('[');

        int maxOptionCount = boundStory.FlattenHierarchie()
                                       .Select(n => n switch
                                       {
                                           BoundChooseStatementNode chooseStatement => chooseStatement.Options.Length,
                                           _ => int.MinValue,
                                       })
                                       .Append(0) // if sequence is empty, at least have one number
                                       .Max();

        writer.Write(maxOptionCount);

        writer.WriteLine("]);");
    }
}
