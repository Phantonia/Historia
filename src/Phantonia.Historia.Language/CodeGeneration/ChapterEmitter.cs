using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class ChapterEmitter(
    StoryNode story,
    FlowGraph flowGraph,
    SymbolTable symbolTable,
    Settings settings,
    ImmutableDictionary<long, IEnumerable<OutcomeSymbol>> definitelyAssignedOutcomesAtChapters,
    IndentedTextWriter writer)
{
    public void GenerateChapterType()
    {
        if (!symbolTable.AllSymbols.Any(s => s is SubroutineSymbol { Kind: SubroutineKind.Chapter, Name: not "main" }))
        {
            return;
        }

        writer.Write("public struct ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Chapter");

        writer.BeginBlock();

        GenerateConstructor(settings, writer);

        writer.WriteLine();
        writer.WriteLine("internal long Index { get; }");
        writer.WriteLine();
        writer.WriteLine("internal bool NeedsStateTransition { get; }");
        writer.WriteLine();

        GenerateOutcomeProperties();

        GenerateGetChapterMethods();

        GenerateIsReadyMethod();

        writer.EndBlock(); // type
    }

    private static void GenerateConstructor(Settings settings, IndentedTextWriter writer)
    {
        writer.Write("private ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Chapter(long index, bool needsStateTransition)");

        writer.BeginBlock();
        writer.WriteLine("Index = index;");
        writer.WriteLine("NeedsStateTransition = needsStateTransition;");
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

    private void GenerateGetChapterMethods()
    {
        IEnumerable<(SubroutineSymbol, SubroutineSymbolDeclarationNode)> chapterDeclarations =
            story.TopLevelNodes
                 .OfType<BoundSymbolDeclarationNode>()
                 .Where(s => s.Symbol is SubroutineSymbol { IsChapter: true, Name: not "main" })
                 .Select(s => ((SubroutineSymbol)s.Symbol, (SubroutineSymbolDeclarationNode)s.Original));

        foreach ((SubroutineSymbol chapter, SubroutineSymbolDeclarationNode declaration) in chapterDeclarations)
        {
            writer.WriteLine("/// <summary>");
            writer.Write("/// Gets a chapter object for chapter '");
            writer.Write(chapter.Name);
            writer.WriteLine("'.");

            if (definitelyAssignedOutcomesAtChapters[chapter.Index].Where(o => o.IsPublic).Any()) {

                writer.WriteLine("/// The following outcomes are required: ");
                writer.WriteLine("""/// <list type="bullet">""");

                foreach (Symbol symbol in symbolTable.AllSymbols)
                {
                    if (symbol is not OutcomeSymbol { IsPublic: true } outcome)
                    {
                        continue;
                    }

                    if (definitelyAssignedOutcomesAtChapters[chapter.Index].Any(o => o.Index == symbol.Index))
                    {
                        writer.Write("/// <item>");
                        writer.Write(symbol.Name);
                        writer.WriteLine("</item>");
                    }
                }

                writer.WriteLine("/// </list>");
            }

            writer.WriteLine("/// Other outcomes might be optional.");
            writer.WriteLine("/// </summary>");

            writer.Write("public static ");
            writer.Write(settings.StoryName);
            writer.Write("Chapter Chapter");
            writer.Write(chapter.Name);
            writer.WriteLine("()");

            writer.BeginBlock();

            // this constraint is not enforced by the language and should not be there
            // need to figure out a cleverer way to find the entry index
            // plus this might not be an exhaustive list of statements that don't result in vertices
            // TODO: find better way
            Debug.Assert(declaration.Body.Statements.Length > 0);
            long entryIndex = declaration.Body.Statements.First(s => s is not BoundOutcomeDeclarationStatementNode or BoundSpectrumDeclarationStatementNode).Index;
            bool needsStateTransition = !flowGraph.Vertices[entryIndex].IsVisible;

            writer.Write(settings.StoryName);
            writer.Write("Chapter instance = new ");
            writer.Write(settings.StoryName);
            writer.Write("Chapter(");
            writer.Write(entryIndex);
            writer.Write(", ");
            writer.Write(needsStateTransition ? "true" : "false");
            writer.WriteLine(");");

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

                if (definitelyAssignedOutcomesAtChapters[chapter.Index].Any(o => o.Index == symbol.Index))
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

            writer.WriteLine("return instance;");

            writer.EndBlock(); // method

            writer.WriteLine();
        }
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
