using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class FieldsEmitter(StoryNode boundStory, SymbolTable symbolTable, IndentedTextWriter writer)
{
    public void GenerateFieldsStruct()
    {
        int initialIndent = writer.Indent;

        writer.WriteLine("internal struct Fields");

        writer.BeginBlock();

        GenerateFields();

        writer.EndBlock(); // struct
        Debug.Assert(writer.Indent == initialIndent);
    }

    private void GenerateFields()
    {
        writer.WriteLine("public long state;");

        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            switch (symbol)
            {
                case SpectrumSymbol spectrum:
                    writer.Write("public int ");
                    GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
                    writer.WriteLine(';');
                    writer.Write("public int ");
                    GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
                    writer.WriteLine(';');
                    break;
                case OutcomeSymbol outcome:
                    writer.Write("public int ");
                    GeneralEmission.GenerateOutcomeFieldName(outcome, writer);

                    if (outcome.DefaultOption is not null)
                    {
                        writer.Write($" = {outcome.OptionNames.IndexOf(outcome.DefaultOption)}");
                    }

                    writer.WriteLine(';');
                    break;
                case CallerTrackerSymbol tracker:
                    writer.Write("public int ");
                    GeneralEmission.GenerateTrackerFieldName(tracker, writer);
                    writer.WriteLine(";");
                    break;
                case ReferenceSymbol reference:
                    writer.Write("public I");
                    writer.Write(reference.Interface.Name);
                    writer.Write(" reference");
                    writer.Write(reference.Name);
                    writer.WriteLine(';');
                    break;
            }
        }

        foreach (LoopSwitchStatementNode loopSwitch in boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>())
        {
            writer.Write("public ulong ");
            GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            writer.WriteLine(';');
        }
    }
}
