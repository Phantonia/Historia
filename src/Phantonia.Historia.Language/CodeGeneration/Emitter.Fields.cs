using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateOutcomeFields(IndentedTextWriter writer)
    {
        foreach (SyntaxNode node in boundStory.FlattenHierarchie())
        {
            if (node is IBoundSpectrumDeclarationNode { Spectrum: SpectrumSymbol spectrum })
            {
                writer.Write("private int ");
                writer.Write(GetSpectrumTotalFieldName(spectrum));
                writer.WriteLine(';');
                writer.Write("private int ");
                writer.Write(GetSpectrumPositiveFieldName(spectrum));
                writer.WriteLine(';');
            }
            else if (node is IBoundOutcomeDeclarationNode { Outcome: OutcomeSymbol outcome })
            {
                writer.Write("private int ");
                writer.Write(GetOutcomeFieldName(outcome));

                if (outcome.DefaultOption is not null)
                {
                    writer.Write($" = {outcome.OptionNames.IndexOf(outcome.DefaultOption)}");
                }

                writer.WriteLine(';');
            }
        }
    }

    private static string GetOutcomeFieldName(OutcomeSymbol outcome) => outcome.Index >= 0 ? $"outcome{outcome.Index}" : $"outcome_{-outcome.Index}";

    private static string GetSpectrumTotalFieldName(SpectrumSymbol spectrum) => spectrum.Index >= 0 ? $"total{spectrum.Index}" : $"total_{-spectrum.Index}";

    private static string GetSpectrumPositiveFieldName(SpectrumSymbol spectrum) => spectrum.Index >= 0 ? $"positive{spectrum.Index}" : $"total_{-spectrum.Index}";
}
