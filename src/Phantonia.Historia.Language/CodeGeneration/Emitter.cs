using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class Emitter(
    StoryNode boundStory,
    Settings settings,
    FlowGraph flowGraph,
    SymbolTable symbolTable,
    ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtChapters,
    TextWriter outputWriter)
{
    private readonly IndentedTextWriter writer = new(outputWriter);

    public void GenerateOutputCode()
    {
        writer.WriteLine("#nullable enable");

        if (settings.Namespace != "")
        {
            writer.Write("namespace ");
            writer.WriteLine(settings.Namespace);
            writer.BeginBlock();
        }

        TypeDeclarationsEmitter typeDeclarationsEmitter = new(boundStory, settings, writer);
        typeDeclarationsEmitter.GenerateTypeDeclarations();

        FieldsEmitter fieldsEmitter = new(boundStory, symbolTable, writer);
        fieldsEmitter.GenerateFieldsStruct();

        writer.WriteLine();

        StateMachineEmitter stateMachineEmitter = new(boundStory, settings, symbolTable, writer);
        stateMachineEmitter.GenerateStateMachineClass();

        writer.WriteLine();

        SnapshotEmitter snapshotEmitter = new(settings, symbolTable, writer);
        snapshotEmitter.GenerateSnapshotClass();

        writer.WriteLine();

        HeartEmitter heartEmitter = new(flowGraph, boundStory, settings, writer);
        heartEmitter.GenerateHeartClass();

        writer.WriteLine();

        StoryGraphEmitter storyGraphEmitter = new(flowGraph, settings, writer);
        storyGraphEmitter.GenerateStoryGraphClass();

        writer.WriteLine();

        ChapterEmitter chapterEmitter = new(boundStory, flowGraph, symbolTable, settings, definitelyAssignedOutcomesAtChapters, writer);
        chapterEmitter.GenerateChapterType();

        if (settings.Namespace != "")
        {
            writer.EndBlock(); // namespace
        }

        Debug.Assert(writer.Indent == 0);
    }
}
