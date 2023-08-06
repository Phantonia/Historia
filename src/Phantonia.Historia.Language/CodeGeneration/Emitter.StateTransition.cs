using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateStateTransitionMethod(IndentedTextWriter writer)
    {
        writer.WriteManyLines(
            """
            private void StateTransition(int option)
            {
                while (true)
                {
                    switch (state, option)
                    {
            """);

        writer.Indent += 3;

        GenerateStartTransition(writer);

        foreach ((int index, ImmutableList<int> edges) in flowGraph.OutgoingEdges)
        {
            switch (flowGraph.Vertices[index].AssociatedStatement)
            {
                case OutputStatementNode:
                    GenerateOutputTransition(writer, index, edges);
                    break;
                case SwitchStatementNode switchStatement:
                    GenerateSwitchTransition(writer, switchStatement, edges);
                    break;
                case BoundBranchOnStatementNode branchOnStatement:
                    GenerateBranchOnTransition(writer, branchOnStatement, edges);
                    break;
                case BoundOutcomeAssignmentStatementNode outcomeAssignment:
                    GenerateOutcomeAssignmentTransition(writer, outcomeAssignment, edges);
                    break;
                case BoundSpectrumAdjustmentStatementNode spectrumAdjustment:
                    GenerateSpectrumAdjustmentTransition(writer, spectrumAdjustment, edges);
                    break;

            }
        }

        writer.Indent -= 3;

        writer.WriteManyLines(
            """
                    }

                    throw new global::System.InvalidOperationException("Invalid state");
                }
            }
            """);

    }

    private void GenerateStartTransition(IndentedTextWriter writer)
    {
        writer.Write("case (");
        writer.Write(StartState);
        writer.WriteLine(", _):");
        writer.Indent++;
        writer.Write("state = ");
        writer.Write(flowGraph.StartVertex);
        writer.WriteLine(';');

        if (flowGraph.StartVertex == EndState || flowGraph.Vertices[flowGraph.StartVertex].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }

        writer.Indent--;
    }

    private void GenerateOutputTransition(IndentedTextWriter writer, int index, ImmutableList<int> edges)
    {
        Debug.Assert(flowGraph.OutgoingEdges[index].Count == 1);

        writer.WriteLine($"case ({index}, _):");

        writer.Indent++;
        writer.WriteLine($"state = {edges[0]};");

        if (edges[0] == EndState || flowGraph.Vertices[edges[0]].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }

        writer.Indent--;
    }

    private void GenerateSwitchTransition(IndentedTextWriter writer, SwitchStatementNode switchStatement, ImmutableList<int> edges)
    {
        Debug.Assert(switchStatement.Options.Length == edges.Count);

        for (int i = 0; i < switchStatement.Options.Length; i++)
        {
            // we know that for each option its index equals that of the associated next vertex
            // we also know that the order that the options appear in the switch statement node is exactly how each one will be indexed
            // plus we know that the outgoing edges are in exactly the right order

            // here we assert that the index of the vertex equals the index of the first statement of the option
            // however we ignore the case where the option's body is empty - there it would be impossible to get the next statement
            Debug.Assert(switchStatement.Options[i].Body.Statements.Length == 0 || switchStatement.Options[i].Body.Statements[0].Index == edges[i]);

            writer.WriteLine($"case ({switchStatement.Index}, {i}):");

            writer.Indent++;
            writer.WriteLine($"state = {edges[i]};");

            if (switchStatement is BoundNamedSwitchStatementNode { Outcome: OutcomeSymbol outcome })
            {
                writer.WriteLine($"{GetOutcomeFieldName(outcome)} = {outcome.OptionNames.IndexOf(switchStatement.Options[i].Name!)};");
            }

            if (edges[i] == EndState || flowGraph.Vertices[edges[i]].IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }

            writer.Indent--;
        }
    }

    private void GenerateBranchOnTransition(IndentedTextWriter writer, BoundBranchOnStatementNode branchOnStatement, ImmutableList<int> edges)
    {
        int index = branchOnStatement.Index;

        writer.WriteLine($"case ({index}, _):");

        writer.Indent++;
        if (branchOnStatement.Outcome is SpectrumSymbol)
        {
            GenerateSpectrumBranchOnTransition(writer, branchOnStatement, edges);
        }
        else
        {
            GenerateOutcomeBranchOnTransition(writer, branchOnStatement, edges);
        }
        writer.Indent--;
    }

    private void GenerateOutcomeBranchOnTransition(IndentedTextWriter writer, BoundBranchOnStatementNode branchOnStatement, ImmutableList<int> edges)
    {
        string outcomeField = GetOutcomeFieldName(branchOnStatement.Outcome);
        writer.WriteLine($"switch ({outcomeField})");
        writer.WriteLine('{');
        writer.Indent++;

        for (int i = 0; i < branchOnStatement.Options.Length; i++)
        {
            BranchOnOptionNode option = branchOnStatement.Options[i];

            if (option is NamedBranchOnOptionNode { OptionName: string optionName })
            {
                int optionIndex = branchOnStatement.Outcome.OptionNames.IndexOf(optionName);
                Debug.Assert(optionIndex >= 0);

                writer.Write("case ");
                writer.Write(optionIndex);
                writer.WriteLine(':');

                writer.Indent++;

                writer.Write("state = ");
                writer.Write(edges[i]);
                writer.WriteLine(';');

                FlowVertex followingVertex = flowGraph.Vertices[edges[i]];
                if (edges[i] == EndState || followingVertex.IsVisible)
                {
                    writer.WriteLine("return;");
                }
                else
                {
                    writer.WriteLine("continue;");
                }

                writer.Indent--;
            }
            else
            {
                writer.WriteLine("default:");
                writer.Indent++;

                int nextState = flowGraph.OutgoingEdges[branchOnStatement.Index][i];
                writer.Write("state = ");
                writer.Write(nextState);
                writer.WriteLine(';');

                if (edges[i] == EndState || flowGraph.Vertices[edges[i]].IsVisible)
                {
                    writer.WriteLine("return;");
                }
                else
                {
                    writer.WriteLine("continue;");
                }
            }
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid outcome\");");
    }

    private void GenerateSpectrumBranchOnTransition(IndentedTextWriter writer, BoundBranchOnStatementNode branchOnStatement, ImmutableList<int> edges)
    {
        Debug.Assert(branchOnStatement.Outcome is SpectrumSymbol);

        SpectrumSymbol spectrum = (SpectrumSymbol)branchOnStatement.Outcome;

        Debug.Assert(spectrum.Intervals.All(i => i.Value.UpperDenominator == spectrum.Intervals.First().Value.UpperDenominator));

        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("int value = ");
        writer.Write(GetSpectrumPositiveFieldName(spectrum));
        writer.Write(" * ");
        writer.Write(spectrum.Intervals.First().Value.UpperDenominator);
        writer.WriteLine(';');

        writer.WriteLine();

        List<BranchOnOptionNode> options = branchOnStatement.Options
                                                            .OfType<NamedBranchOnOptionNode>()
                                                            .OrderBy(o => spectrum.Intervals[o.OptionName].UpperNumerator)
                                                            .ThenBy(o => spectrum.Intervals[o.OptionName].Inclusive)
                                                            .Cast<BranchOnOptionNode>()
                                                            .ToList();

        if (branchOnStatement.Options[^1] is OtherBranchOnOptionNode)
        {
            options.Add(branchOnStatement.Options[^1]);
        }

        for (int i = 0; i < options.Count - 1; i++)
        {
            BranchOnOptionNode option = options[i];

            Debug.Assert(option is NamedBranchOnOptionNode); // only the last option may not be named
            string optionName = ((NamedBranchOnOptionNode)option).OptionName;

            SpectrumInterval interval = spectrum.Intervals[optionName];

            writer.Write("if (value <");

            if (interval.Inclusive)
            {
                writer.Write('=');
            }

            writer.Write(' ');
            writer.Write(GetSpectrumTotalFieldName(spectrum));
            writer.Write(" * ");
            writer.Write(interval.UpperNumerator);
            writer.WriteLine(')');
            writer.WriteLine('{');
            writer.Indent++;
            writer.Write("state = ");

            int index = branchOnStatement.Options.IndexOf(option);
            writer.Write(edges[index]);
            writer.WriteLine(';');

            if (edges[i] == EndState || flowGraph.Vertices[edges[i]].IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }

            writer.Indent--;
            writer.WriteLine('}');
            writer.Write("else ");
        }

        writer.WriteLine();
        writer.WriteLine('{');
        writer.Indent++;

        writer.Write("state = ");
        writer.Write(edges[branchOnStatement.Options.IndexOf(options[^1])]);
        writer.WriteLine(';');

        if (edges[^1] == EndState || flowGraph.Vertices[edges[^1]].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }

        writer.Indent--;
        writer.WriteLine('}');

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateOutcomeAssignmentTransition(IndentedTextWriter writer, BoundOutcomeAssignmentStatementNode outcomeAssignment, ImmutableList<int> edges)
    {
        writer.WriteLine($"case ({outcomeAssignment.Index}, _):");
        writer.Indent++;
        writer.Write(GetOutcomeFieldName(outcomeAssignment.Outcome));
        writer.Write(" = ");
        writer.Write(outcomeAssignment.Outcome.OptionNames.IndexOf(outcomeAssignment.AssignedOption));
        writer.WriteLine(";");
        writer.WriteLine($"state = {flowGraph.OutgoingEdges[outcomeAssignment.Index][0]};");
        writer.WriteLine("continue;");
        writer.Indent--;
    }

    private void GenerateSpectrumAdjustmentTransition(IndentedTextWriter writer, BoundSpectrumAdjustmentStatementNode spectrumAdjustment, ImmutableList<int> edges)
    {
        int index = spectrumAdjustment.Index;

        writer.Write("case (");
        writer.Write(index);
        writer.WriteLine(", _):");
        writer.Indent++;
        writer.Write(GetSpectrumTotalFieldName(spectrumAdjustment.Spectrum));
        writer.Write(" += ");
        GenerateExpression(writer, spectrumAdjustment.AdjustmentAmount);
        writer.WriteLine(';');

        if (spectrumAdjustment.Strengthens)
        {
            writer.Write(GetSpectrumPositiveFieldName(spectrumAdjustment.Spectrum));
            writer.Write(" += ");
            GenerateExpression(writer, spectrumAdjustment.AdjustmentAmount);
            writer.WriteLine(';');
        }

        writer.WriteLine($"state = {edges[0]};");

        if (edges[0] == EndState || flowGraph.Vertices[edges[0]].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }

        writer.Indent--;
    }
}
