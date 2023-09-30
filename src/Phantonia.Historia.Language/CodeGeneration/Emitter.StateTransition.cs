using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateStateTransitionMethod()
    {
        writer.WriteManyLines(
            """
            private void StateTransition(int option)
            {
                while (true)
                {
                    switch (state)
                    {
            """);

        writer.Indent += 3;

        writer.Write("case ");
        writer.Write(StartState);
        writer.WriteLine(':');
        writer.Indent++;

        GenerateStartTransition();

        writer.Indent--;

        foreach ((int index, ImmutableList<FlowEdge> edges) in flowGraph.OutgoingEdges)
        {
            writer.Write("case ");
            writer.Write(index);
            writer.WriteLine(':');
            writer.Indent++;

            switch (flowGraph.Vertices[index].AssociatedStatement)
            {
                case OutputStatementNode:
                    GenerateOutputTransition(index, edges);
                    break;
                case SwitchStatementNode switchStatement:
                    GenerateSwitchTransition(switchStatement, edges);
                    break;
                case BoundBranchOnStatementNode branchOnStatement:
                    GenerateBranchOnTransition(branchOnStatement, edges);
                    break;
                case BoundOutcomeAssignmentStatementNode outcomeAssignment:
                    GenerateOutcomeAssignmentTransition(outcomeAssignment, edges);
                    break;
                case BoundSpectrumAdjustmentStatementNode spectrumAdjustment:
                    GenerateSpectrumAdjustmentTransition(spectrumAdjustment, edges);
                    break;
                case CallerTrackerStatementNode trackerStatement:
                    GenerateCallerTrackerTransition(trackerStatement, edges);
                    break;
                case CallerResolutionStatementNode resolutionStatement:
                    GenerateCallerResolutionTransition(resolutionStatement, edges);
                    break;
            }

            writer.Indent--;
        }

        writer.Indent -= 3;

        writer.WriteManyLines(
            """
                    }

                    throw new global::System.InvalidOperationException("Fatal internal error: Invalid state");
                }
            }
            """);

    }

    private void GenerateStartTransition()
    {
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
    }

    private void GenerateOutputTransition(int index, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(flowGraph.OutgoingEdges[index].Count == 1);

        writer.WriteLine($"state = {edges[0].ToVertex};");

        if (edges[0].ToVertex == EndState || flowGraph.Vertices[edges[0].ToVertex].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }
    }

    private void GenerateSwitchTransition(SwitchStatementNode switchStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(switchStatement.Options.Length == edges.Count);

        writer.WriteLine("switch (option)");
        writer.WriteLine('{');
        writer.Indent++;

        for (int i = 0; i < switchStatement.Options.Length; i++)
        {
            // we know that for each option its index equals that of the associated next vertex
            // we also know that the order that the options appear in the switch statement node is exactly how each one will be indexed
            // plus we know that the outgoing edges are in exactly the right order

            // here we assert that the index of the vertex equals the index of the first statement of the option
            // however we ignore the case where the option's body is empty - there it would be impossible to get the next statement
            Debug.Assert(switchStatement.Options[i].Body.Statements.Length == 0 || switchStatement.Options[i].Body.Statements[0].Index == edges[i].ToVertex);

            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');

            writer.Indent++;

            writer.Write("state = ");
            writer.Write(edges[i].ToVertex);
            writer.WriteLine(';');

            if (switchStatement is BoundNamedSwitchStatementNode { Outcome: OutcomeSymbol outcome })
            {
                WriteOutcomeFieldName(outcome);
                writer.Write(" = ");
                writer.Write(outcome.OptionNames.IndexOf(switchStatement.Options[i].Name!));
                writer.WriteLine(';');
            }

            if (edges[i].ToVertex == EndState || flowGraph.Vertices[edges[i].ToVertex].IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }

            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("break;"); // C# is so weird - you cannot fall from a case label so they require you to slap a 'break' at the end instead of just not doing that smh
    }

    private void GenerateBranchOnTransition(BoundBranchOnStatementNode branchOnStatement, ImmutableList<FlowEdge> edges)
    {
        if (branchOnStatement.Outcome is SpectrumSymbol)
        {
            GenerateSpectrumBranchOnTransition(branchOnStatement, edges);
        }
        else
        {
            GenerateOutcomeBranchOnTransition(branchOnStatement, edges);
        }
    }

    private void GenerateOutcomeBranchOnTransition(BoundBranchOnStatementNode branchOnStatement, ImmutableList<FlowEdge> edges)
    {
        writer.Write("switch (");
        WriteOutcomeFieldName(branchOnStatement.Outcome);
        writer.WriteLine(')');
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
                writer.Write(edges[i].ToVertex);
                writer.WriteLine(';');

                FlowVertex followingVertex = flowGraph.Vertices[edges[i].ToVertex];
                if (edges[i].ToVertex == EndState || followingVertex.IsVisible)
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

                int nextState = flowGraph.OutgoingEdges[branchOnStatement.Index][i].ToVertex;
                writer.Write("state = ");
                writer.Write(nextState);
                writer.WriteLine(';');

                if (edges[i].ToVertex == EndState || flowGraph.Vertices[edges[i].ToVertex].IsVisible)
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
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Fatal internal error: Invalid outcome\");");
    }

    private void GenerateSpectrumBranchOnTransition(BoundBranchOnStatementNode branchOnStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(branchOnStatement.Outcome is SpectrumSymbol);

        SpectrumSymbol spectrum = (SpectrumSymbol)branchOnStatement.Outcome;

        Debug.Assert(spectrum.Intervals.All(i => i.Value.UpperDenominator == spectrum.Intervals.First().Value.UpperDenominator));

        writer.WriteLine('{');
        writer.Indent++;

        if (spectrum.DefaultOption is not null)
        {
            writer.Write("if (");
            WriteSpectrumTotalFieldName(spectrum);
            writer.WriteLine(" == 0)");
            writer.WriteLine('{');
            writer.Indent++;

            int? nextState = null;

            for (int i = 0; i < branchOnStatement.Options.Length; i++)
            {
                if (branchOnStatement.Options[i] is NamedBranchOnOptionNode { OptionName: string optionName } && optionName == spectrum.DefaultOption)
                {
                    nextState = edges[i].ToVertex;
                }
            }

            nextState ??= edges[^1].ToVertex; // other option needs to be last

            writer.Write("state = ");
            writer.Write((int)nextState);
            writer.WriteLine(';');

            if (nextState == EndState || flowGraph.Vertices[(int)nextState].IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }

            writer.Indent--;
            writer.WriteLine('}');
        }

        writer.Write("int value = ");
        WriteSpectrumPositiveFieldName(spectrum);
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
            WriteSpectrumTotalFieldName(spectrum);
            writer.Write(" * ");
            writer.Write(interval.UpperNumerator);
            writer.WriteLine(')');
            writer.WriteLine('{');
            writer.Indent++;
            writer.Write("state = ");

            int index = branchOnStatement.Options.IndexOf(option);
            writer.Write(edges[index].ToVertex);
            writer.WriteLine(';');

            if (edges[i].ToVertex == EndState || flowGraph.Vertices[edges[i].ToVertex].IsVisible)
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
        writer.Write(edges[branchOnStatement.Options.IndexOf(options[^1])].ToVertex);
        writer.WriteLine(';');

        if (edges[^1].ToVertex == EndState || flowGraph.Vertices[edges[^1].ToVertex].IsVisible)
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

    private void GenerateOutcomeAssignmentTransition(BoundOutcomeAssignmentStatementNode outcomeAssignment, ImmutableList<FlowEdge> edges)
    {
        WriteOutcomeFieldName(outcomeAssignment.Outcome);
        writer.Write(" = ");
        writer.Write(outcomeAssignment.Outcome.OptionNames.IndexOf(outcomeAssignment.AssignedOption));
        writer.WriteLine(";");

        Debug.Assert(edges.Count == 1);

        writer.Write("state = ");
        writer.Write(edges[0].ToVertex);
        writer.WriteLine(';');

        if (edges[0].ToVertex == EndState || flowGraph.Vertices[edges[0].ToVertex].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }
    }

    private void GenerateSpectrumAdjustmentTransition(BoundSpectrumAdjustmentStatementNode spectrumAdjustment, ImmutableList<FlowEdge> edges)
    {
        WriteSpectrumTotalFieldName(spectrumAdjustment.Spectrum);
        writer.Write(" += ");
        GenerateExpression(spectrumAdjustment.AdjustmentAmount);
        writer.WriteLine(';');

        if (spectrumAdjustment.Strengthens)
        {
            WriteSpectrumPositiveFieldName(spectrumAdjustment.Spectrum);
            writer.Write(" += ");
            GenerateExpression(spectrumAdjustment.AdjustmentAmount);
            writer.WriteLine(';');
        }

        Debug.WriteLine(edges.Count == 1);

        writer.WriteLine($"state = {edges[0].ToVertex};");

        if (edges[0].ToVertex == EndState || flowGraph.Vertices[edges[0].ToVertex].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }
    }

    private void GenerateCallerTrackerTransition(CallerTrackerStatementNode trackerStatement, ImmutableList<FlowEdge> edges)
    {
        WriteTrackerFieldName(trackerStatement.Tracker);
        writer.Write(" = ");
        writer.Write(trackerStatement.CallSiteIndex);
        writer.WriteLine(';');

        writer.WriteLine($"state = {edges[0].ToVertex};");

        if (edges[0].ToVertex == EndState || flowGraph.Vertices[edges[0].ToVertex].IsVisible)
        {
            writer.WriteLine("return;");
        }
        else
        {
            writer.WriteLine("continue;");
        }
    }

    private void GenerateCallerResolutionTransition(CallerResolutionStatementNode resolutionStatement, ImmutableList<FlowEdge> edges)
    {
        writer.Write("switch (");
        WriteTrackerFieldName(resolutionStatement.Tracker);
        writer.WriteLine(')');

        writer.WriteLine('{');
        writer.Indent++;

        for (int i = 0; i < resolutionStatement.Tracker.CallSiteCount; i++)
        {
            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');
            writer.Indent++;

            writer.Write("state = ");
            writer.Write(edges[i].ToVertex);
            writer.WriteLine(';');

            FlowVertex followingVertex = flowGraph.Vertices[edges[i].ToVertex];
            if (edges[i].ToVertex == EndState || followingVertex.IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }

            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Fatal internal error: Invalid call site\");");
    }
}
