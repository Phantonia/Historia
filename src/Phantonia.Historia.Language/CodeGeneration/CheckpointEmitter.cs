using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class CheckpointEmitter(
    FlowGraph flowGraph,
    SymbolTable symbolTable,
    Settings settings,
    ImmutableDictionary<int, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtCheckpoints,
    IndentedTextWriter writer)
{
    public void GenerateCheckpointType()
    {
        if (!flowGraph.Vertices.Any(v => v.Value.IsCheckpoint))
        {
            return;
        }

        writer.Write("public struct ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint");

        writer.BeginBlock();

        GenerateOutcomeProperties();

        GenerateGetForIndexMethod();

        writer.EndBlock();
    }

    private void GenerateOutcomeProperties()
    {
        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            if (symbol is not OutcomeSymbol { IsPublic: true })
            {
                continue;
            }

            if (symbol is SpectrumSymbol)
            {
                writer.Write("public global::Phantonia.Historia.CheckpointSpectrum ");
                writer.Write(symbol.Name);
                writer.WriteLine(" { get; set; }");

                writer.WriteLine();

                continue;
            }

            writer.Write("public global::Phantonia.Historia.CheckpointOutcome<");
            writer.Write(symbol is SpectrumSymbol ? "Spectrum" : "Outcome");
            writer.Write(symbol.Name);
            writer.Write("> ");
            writer.Write(symbol.Name);
            writer.WriteLine(" { get; set; }");

            writer.WriteLine();
        }
    }

    private void GenerateGetForIndexMethod()
    {
        writer.Write("public static ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint GetForIndex(int index)");

        writer.BeginBlock();

        writer.Write(settings.StoryName);
        writer.Write("Checkpoint instance = new ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint();");

        writer.WriteLine();

        writer.WriteLine("switch (index)");

        writer.BeginBlock();

        foreach (FlowVertex vertex in flowGraph.Vertices.Values)
        {
            if (!vertex.IsCheckpoint)
            {
                continue;
            }

            writer.Write("case ");
            writer.Write(vertex.Index);
            writer.WriteLine(':');
            writer.Indent++;

            foreach (Symbol symbol in symbolTable.AllSymbols)
            {
                if (symbol is not OutcomeSymbol { IsPublic: true })
                {
                    continue;
                }

                writer.Write("instance.");
                writer.Write(symbol.Name);
                writer.Write(" = ");

                writer.Write("global::Phantonia.Historia.");

                if (symbol is SpectrumSymbol)
                {
                    writer.Write("CheckpointSpectrum");
                }
                else
                {
                    writer.Write("CheckpointOutcome<");
                    writer.Write("Outcome");
                    writer.Write(symbol.Name);
                    writer.Write('>');
                }

                writer.Write('.');

                if (definitelyAssignedOutcomesAtCheckpoints[vertex.Index].Any(o => o.Index == symbol.Index))
                {
                    writer.Write("Required");
                }
                else
                {
                    writer.Write("NotRequired");
                }

                writer.WriteLine("();");
            }

            writer.WriteLine("break;");

            writer.Indent--;
        }

        writer.WriteLine("default:");
        writer.Indent++;
        writer.WriteLine("throw new global::System.ArgumentException(\"index \" + index + \" is not a checkpoint\");");
        writer.Indent--;

        writer.EndBlock();

        writer.WriteLine();
        writer.WriteLine("return instance;");

        writer.EndBlock();
    }
}
