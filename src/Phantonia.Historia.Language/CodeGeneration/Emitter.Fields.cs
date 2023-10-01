using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateFields()
    {
        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            switch (symbol)
            {
                case SpectrumSymbol spectrum:
                    writer.Write("private int ");
                    WriteSpectrumTotalFieldName(spectrum);
                    writer.WriteLine(';');
                    writer.Write("private int ");
                    WriteSpectrumPositiveFieldName(spectrum);
                    writer.WriteLine(';');
                    break;
                case OutcomeSymbol outcome:
                    writer.Write("private int ");
                    WriteOutcomeFieldName(outcome);

                    if (outcome.DefaultOption is not null)
                    {
                        writer.Write($" = {outcome.OptionNames.IndexOf(outcome.DefaultOption)}");
                    }

                    writer.WriteLine(';');
                    break;
                case CallerTrackerSymbol tracker:
                    writer.Write("private int ");
                    WriteTrackerFieldName(tracker);
                    writer.WriteLine(";");
                    break;
            }
        }

        foreach (LoopSwitchStatementNode loopSwitch in boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>())
        {
            writer.Write("private ulong ");
            WriteLoopSwitchFieldName(loopSwitch);
            writer.WriteLine(" = 0;");
        }
    }

    private void WriteOutcomeFieldName(OutcomeSymbol outcome)
    {
        writer.Write("outcome");

        if (outcome.Index >= 0)
        {
            writer.Write(outcome.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-outcome.Index);
        }
    }

    private void WriteSpectrumTotalFieldName(SpectrumSymbol spectrum)
    {
        writer.Write("total");

        if (spectrum.Index >= 0)
        {
            writer.Write(spectrum.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-spectrum.Index);
        }
    }

    private void WriteSpectrumPositiveFieldName(SpectrumSymbol spectrum)
    {
        writer.Write("positive");

        if (spectrum.Index >= 0)
        {
            writer.Write(spectrum.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-spectrum.Index);
        }
    }

    private void WriteTrackerFieldName(CallerTrackerSymbol tracker)
    {
        writer.Write("tracker");

        if (tracker.Index >= 0)
        {
            writer.Write(tracker.Index);
        }
        else
        {
            writer.Write('_');
            writer.Write(-tracker.Index);
        }
    }

    private string GetLoopSwitchFieldName(LoopSwitchStatementNode loopSwitch) => $"ls{loopSwitch.Index}";

    private void WriteLoopSwitchFieldName(LoopSwitchStatementNode loopSwitch)
    {
        writer.Write("ls");
        writer.Write(loopSwitch.Index);
    }
}
