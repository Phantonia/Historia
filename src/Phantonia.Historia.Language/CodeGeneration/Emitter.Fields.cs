using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateOutcomeFields()
    {
        foreach (Symbol symbol in symbolTable.AllSymbols)
        {
            switch (symbol)
            {
                case SpectrumSymbol spectrum:
                    writer.Write("private int ");
                    writer.Write(GetSpectrumTotalFieldName(spectrum));
                    writer.WriteLine(';');
                    writer.Write("private int ");
                    writer.Write(GetSpectrumPositiveFieldName(spectrum));
                    writer.WriteLine(';');
                    break;
                case OutcomeSymbol outcome:
                    writer.Write("private int ");
                    writer.Write(GetOutcomeFieldName(outcome));

                    if (outcome.DefaultOption is not null)
                    {
                        writer.Write($" = {outcome.OptionNames.IndexOf(outcome.DefaultOption)}");
                    }

                    writer.WriteLine(';');
                    break;
                case CallerTrackerSymbol tracker:
                    writer.Write("private int ");
                    writer.Write(GetTrackerFieldName(tracker));
                    writer.WriteLine(";");
                    break;
            }
        }
    }

    private static string GetOutcomeFieldName(OutcomeSymbol outcome) => outcome.Index >= 0 ? $"outcome{outcome.Index}" : $"outcome_{-outcome.Index}";

    private static string GetSpectrumTotalFieldName(SpectrumSymbol spectrum) => spectrum.Index >= 0 ? $"total{spectrum.Index}" : $"total_{-spectrum.Index}";

    private static string GetTrackerFieldName(CallerTrackerSymbol tracker) => tracker.Index >= 0 ? $"tracker{tracker.Index}" : $"tracker_{-tracker.Index}";

    private static string GetSpectrumPositiveFieldName(SpectrumSymbol spectrum) => spectrum.Index >= 0 ? $"positive{spectrum.Index}" : $"total_{-spectrum.Index}";
}
