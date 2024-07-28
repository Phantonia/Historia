using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class Emitter
{
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
            writer.BeginBlock();
        }

        TypeDeclarationsEmitter typeDeclarationsEmitter = new(boundStory, writer);
        typeDeclarationsEmitter.GenerateTypeDeclarations();

        writer.WriteLine();

        FieldsEmitter fieldsEmitter = new(boundStory, symbolTable, settings, writer);
        fieldsEmitter.GenerateFieldsStruct();

        writer.WriteLine();

        StateMachineEmitter stateMachineEmitter = new(boundStory, settings, symbolTable, writer);
        stateMachineEmitter.GenerateStateMachineClass();

        writer.WriteLine();

        SnapshotEmitter snapshotEmitter = new(boundStory, settings, symbolTable, writer);
        snapshotEmitter.GenerateSnapshotClass();

        writer.WriteLine();

        HeartEmitter heartEmitter = new(flowGraph, settings, writer);
        heartEmitter.GenerateHeartClass();

        writer.WriteLine();

        StoryGraphEmitter storyGraphEmitter = new(flowGraph, settings, writer);
        storyGraphEmitter.GenerateStoryGraphClass();

        if (settings.Namespace != "")
        {
            writer.EndBlock(); // namespace
        }

        Debug.Assert(writer.Indent == 0);
    }
}
