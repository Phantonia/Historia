using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class CheckpointEmitter(
    FlowGraph flowGraph,
    SymbolTable symbolTable,
    Settings settings,
    ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtCheckpoints,
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

        GenerateConstructor(settings, writer);

        writer.WriteLine();
        writer.WriteLine("public long Index { get; }");
        writer.WriteLine();

        GenerateOutcomeProperties();

        GenerateGetForIndexMethod();

        writer.WriteLine();

        GenerateIsReadyMethod();

        writer.EndBlock();
    }

    private static void GenerateConstructor(Settings settings, IndentedTextWriter writer)
    {
        writer.Write("private ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint(long index)");

        writer.BeginBlock();
        writer.WriteLine("Index = index;");
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
                writer.Write("public global::Phantonia.Historia.CheckpointSpectrum Spectrum");
                writer.Write(symbol.Name);
                writer.WriteLine(" { get; set; }");

                writer.WriteLine();

                continue;
            }

            writer.Write("public global::Phantonia.Historia.CheckpointOutcome<");
            writer.Write("Outcome");
            writer.Write(symbol.Name);
            writer.Write("> Outcome");
            writer.Write(symbol.Name);
            writer.WriteLine(" { get; set; }");

            writer.WriteLine();
        }
    }

    private void GenerateGetForIndexMethod()
    {
        writer.Write("public static ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint GetForIndex(long index)");

        writer.BeginBlock();

        writer.Write(settings.StoryName);
        writer.Write("Checkpoint instance = new ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Checkpoint(index);");

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
                if (symbol is not OutcomeSymbol { IsPublic: true } outcome)
                {
                    continue;
                }

                writer.Write("instance.");
                writer.Write(symbol is SpectrumSymbol ? "Spectrum" : "Outcome");
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
                else if (outcome.DefaultOption is not null)
                {
                    writer.Write("Optional");
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

    private void GenerateIsReadyMethod()
    {
        writer.WriteLine("public bool IsReady()");

        writer.BeginBlock();

        IEnumerable<Symbol> publicOutcomes = symbolTable.AllSymbols.Where(s => s is OutcomeSymbol { IsPublic: true });

        if (!publicOutcomes.Any())
        {
            writer.WriteLine("return true;");
            writer.EndBlock();
            return;
        }

        writer.Write("return ");

        void GenerateTerm(Symbol symbol)
        {
            if (symbol is SpectrumSymbol)
            {
                writer.Write("(Spectrum");
                writer.Write(symbol.Name);
                writer.Write(".Kind != global::Phantonia.Historia.CheckpointOutcomeKind.Required || Spectrum");
                writer.Write(symbol.Name);
                writer.Write(".TotalCount != 0)");

                return;
            }

            // because the property and enum have the name, we can't go OutcomeX.Unset, because that refers to the property
            // instead we need to go global::OutcomeX.Unset
            // however, we could possibly have a whole fucking namespace in between
            // (this btw is the first time we *actually* need global::)
            writer.Write("(Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Kind != global::Phantonia.Historia.CheckpointOutcomeKind.Required || Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Option != global::");
            writer.Write(settings.Namespace);

            if (settings.Namespace != "")
            {
                writer.Write('.');
            }

            writer.Write("Outcome");
            writer.Write(symbol.Name);
            writer.Write(".Unset)");
        }

        GenerateTerm(publicOutcomes.First());

        foreach (Symbol symbol in publicOutcomes.Skip(1))
        {
            writer.Write(" && ");
            GenerateTerm(symbol);
        }

        writer.WriteLine(';');

        writer.EndBlock();
    }
}
