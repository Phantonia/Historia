using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class StateTransitionEmitter(FlowGraph flowGraph, Settings settings, IndentedTextWriter writer)
{
    public void GenerateStateTransitionMethod()
    {
        writer.WriteLine("public static void StateTransition(ref Fields fields, int option)");
        writer.BeginBlock();

        writer.WriteLine("while (true)");
        writer.BeginBlock();

        GenerateSwitch();

        writer.EndBlock(); // while

        writer.EndBlock(); // method
    }

    private void GenerateSwitch()
    {
        writer.WriteLine("switch (fields.state)");
        writer.BeginBlock();

        writer.Write("case ");
        writer.Write(Constants.StartState);
        writer.WriteLine(':');
        writer.Indent++;

        GenerateStartTransition();

        writer.Indent--;

        foreach ((int index, ImmutableList<FlowEdge> edges) in flowGraph.OutgoingEdges)
        {
            if (!flowGraph.Vertices[index].IsStory) // purely semantic vertex
            {
                continue;
            }

            writer.Write("case ");
            writer.Write(index);
            writer.WriteLine(':');
            writer.Indent++;

            switch (flowGraph.Vertices[index].AssociatedStatement)
            {
                case OutputStatementNode:
                    GenerateOutputTransition(index, edges);
                    break;
                case FlowBranchingStatementNode { Original: SwitchStatementNode switchStatement, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges }:
                    GenerateSwitchTransition(switchStatement, outgoingEdges);
                    break;
                case FlowBranchingStatementNode { Original: LoopSwitchStatementNode loopSwitchStatement, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges }:
                    GenerateLoopSwitchTransition(loopSwitchStatement, outgoingEdges);
                    break;
                case FlowBranchingStatementNode { Original: BoundBranchOnStatementNode branchOnStatement, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges }:
                    GenerateBranchOnTransition(branchOnStatement, outgoingEdges);
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
                case BoundRunStatementNode runStatement:
                    GenerateRunTransition(runStatement, edges);
                    break;
                case FlowBranchingStatementNode { Original: BoundChooseStatementNode chooseStatement, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges }:
                    GenerateChooseTransition(chooseStatement, outgoingEdges);
                    break;
                case FlowBranchingStatementNode { Original: IfStatementNode ifStatement, OutgoingEdges: ImmutableList<FlowEdge> outgoingEdges }:
                    GenerateIfTransition(ifStatement, outgoingEdges);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            writer.Indent--;
        }

        writer.EndBlock(); // switch

        writer.WriteLine();
        writer.WriteLine(@"throw new global::System.InvalidOperationException(""Fatal internal error: Invalid state (StateTransition)"");");
    }

    private void GenerateStartTransition()
    {
        Debug.Assert(flowGraph.IsConformable);

        int startVertex = flowGraph.StartEdges.Single(e => e.IsStory).ToVertex;

        writer.Write("fields.state = ");
        writer.Write(startVertex);
        writer.WriteLine(';');

        if (startVertex == Constants.EndState || flowGraph.Vertices[startVertex].IsVisible)
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
        Debug.Assert(flowGraph.OutgoingEdges[index].Count(o => o.IsStory) == 1);
        GenerateTransitionTo(edges.First(e => e.IsStory).ToVertex);
    }

    private void GenerateSwitchTransition(SwitchStatementNode switchStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(switchStatement.Options.Length == edges.Count);

        writer.WriteLine("switch (option)");
        writer.BeginBlock();

        for (int i = 0; i < switchStatement.Options.Length; i++)
        {
            // we know that for each option its index equals that of the associated next vertex
            // we also know that the order that the options appear in the switch statement node is exactly how each one will be indexed
            // plus we know that the outgoing edges are in exactly the right order

            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');

            writer.Indent++;

            GenerateTransitionTo(edges[i].ToVertex);

            writer.Indent--;
        }

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("break;"); // C# is so weird - you cannot fall from a case label so they require you to slap a 'break' at the end instead of just not doing that smh
    }

    private void GenerateLoopSwitchTransition(LoopSwitchStatementNode loopSwitchStatement, ImmutableList<FlowEdge> edges)
    {
        writer.BeginBlock();

        // we do some bitwise black magic
        // ((ls & (1UL << i)) == 0) is true if the i'th bit is zero
        writer.WriteManyLines(
            $$"""
            int tempOption = option;
            int realOption = 0;

            for (int i = 0; i < 64; i++)
            {
                if ((fields.{{GeneralEmission.GetLoopSwitchFieldName(loopSwitchStatement)}} & (1UL << i)) == 0)
                {
                    tempOption--;
                }

                if (tempOption < 0)
                {
                    break;
                }

                realOption++;
            }

            switch (realOption)
            {
            """);
        writer.Indent++;

        for (int i = 0; i < loopSwitchStatement.Options.Length; i++)
        {
            // for comment see switch version
            //Debug.Assert(loopSwitchStatement.Options[i].Body.Statements.Length == 0 || loopSwitchStatement.Options[i].Body.Statements[0].Index == edges[i].ToVertex);

            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');

            writer.Indent++;

            switch (loopSwitchStatement.Options[i].Kind)
            {
                case LoopSwitchOptionKind.None:
                    // for example: ls |= 1 << 3;
                    // we record that this option has been taken, blocking it for the future
                    writer.Write("fields.");
                    GeneralEmission.GenerateLoopSwitchFieldName(loopSwitchStatement, writer);
                    writer.Write(" |= 1 << ");
                    writer.Write(i);
                    writer.WriteLine(';');
                    break;
                case LoopSwitchOptionKind.Final:
                    // reset loop switch field
                    writer.Write("fields.");
                    GeneralEmission.GenerateLoopSwitchFieldName(loopSwitchStatement, writer);
                    writer.WriteLine(" = 0;");
                    break;
            }

            GenerateTransitionTo(edges[i].ToVertex);

            writer.Indent--;
        }

        writer.EndBlock(); // switch

        writer.WriteLine();
        writer.WriteLine("break;"); // mandatory C# break

        writer.EndBlock(); // scope
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
        writer.Write("switch (fields.");
        GeneralEmission.GenerateOutcomeFieldName(branchOnStatement.Outcome, writer);
        writer.WriteLine(')');
        writer.BeginBlock();

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

                GenerateTransitionTo(edges[i].ToVertex);

                writer.Indent--;
            }
            else
            {
                writer.WriteLine("default:");
                writer.Indent++;

                int nextState = flowGraph.OutgoingEdges[branchOnStatement.Index][i].ToVertex;
                GenerateTransitionTo(nextState);
            }
        }

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Fatal internal error: Invalid outcome\");");
    }

    private void GenerateSpectrumBranchOnTransition(BoundBranchOnStatementNode branchOnStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(branchOnStatement.Outcome is SpectrumSymbol);

        SpectrumSymbol spectrum = (SpectrumSymbol)branchOnStatement.Outcome;

        Debug.Assert(spectrum.Intervals.All(i => i.Value.UpperDenominator == spectrum.Intervals.First().Value.UpperDenominator));

        writer.BeginBlock();

        if (spectrum.DefaultOption is not null)
        {
            writer.Write("if (fields.");
            GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
            writer.WriteLine(" == 0)");
            writer.BeginBlock();

            int? nextState = null;

            for (int i = 0; i < branchOnStatement.Options.Length; i++)
            {
                if (branchOnStatement.Options[i] is NamedBranchOnOptionNode { OptionName: string optionName } && optionName == spectrum.DefaultOption)
                {
                    nextState = edges[i].ToVertex;
                }
            }

            nextState ??= edges[^1].ToVertex; // other option needs to be last

            GenerateTransitionTo((int)nextState);

            writer.EndBlock(); // if
        }

        writer.Write("int value = fields.");
        GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
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

            writer.Write(" fields.");
            GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
            writer.Write(" * ");
            writer.Write(interval.UpperNumerator);
            writer.WriteLine(')');
            writer.BeginBlock();

            int index = branchOnStatement.Options.IndexOf(option);
            GenerateTransitionTo(edges[index].ToVertex);

            writer.EndBlock(); // if/else if
            writer.Write("else ");
        }

        writer.WriteLine();
        writer.BeginBlock();

        GenerateTransitionTo(edges[branchOnStatement.Options.IndexOf(options[^1])].ToVertex);

        writer.EndBlock(); // else

        writer.EndBlock(); // scope
    }

    private void GenerateOutcomeAssignmentTransition(BoundOutcomeAssignmentStatementNode outcomeAssignment, ImmutableList<FlowEdge> edges)
    {
        writer.Write("fields.");
        GeneralEmission.GenerateOutcomeFieldName(outcomeAssignment.Outcome, writer);
        writer.Write(" = ");
        writer.Write(outcomeAssignment.Outcome.OptionNames.IndexOf(outcomeAssignment.AssignedOption));
        writer.WriteLine(";");

        Debug.Assert(edges.Count == 1);

        GenerateTransitionTo(edges.First(e => e.IsStory).ToVertex);
    }

    private void GenerateSpectrumAdjustmentTransition(BoundSpectrumAdjustmentStatementNode spectrumAdjustment, ImmutableList<FlowEdge> edges)
    {
        SpectrumSymbol spectrum = spectrumAdjustment.Spectrum;

        writer.Write("fields.");
        GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
        writer.Write(" += ");
        GeneralEmission.GenerateExpression(spectrumAdjustment.AdjustmentAmount, writer);
        writer.WriteLine(';');

        if (spectrumAdjustment.Strengthens)
        {
            writer.Write("fields.");
            GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
            writer.Write(" += ");
            GeneralEmission.GenerateExpression(spectrumAdjustment.AdjustmentAmount, writer);
            writer.WriteLine(';');
        }

        Debug.WriteLine(edges.Count == 1);

        GenerateTransitionTo(edges.First(e => e.IsStory).ToVertex);
    }

    private void GenerateCallerTrackerTransition(CallerTrackerStatementNode trackerStatement, ImmutableList<FlowEdge> edges)
    {
        writer.Write("fields.");
        GeneralEmission.GenerateTrackerFieldName(trackerStatement.Tracker, writer);
        writer.Write(" = ");
        writer.Write(trackerStatement.CallSiteIndex);
        writer.WriteLine(';');

        GenerateTransitionTo(edges.First(e => e.IsStory).ToVertex);
    }

    private void GenerateCallerResolutionTransition(CallerResolutionStatementNode resolutionStatement, ImmutableList<FlowEdge> edges)
    {
        writer.Write("switch (fields.");
        GeneralEmission.GenerateTrackerFieldName(resolutionStatement.Tracker, writer);
        writer.WriteLine(')');

        writer.BeginBlock();

        for (int i = 0; i < resolutionStatement.Tracker.CallSiteCount; i++)
        {
            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');
            writer.Indent++;

            GenerateTransitionTo(edges[i].ToVertex);

            writer.Indent--;
        }

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Fatal internal error: Invalid call site\");");
    }

    private void GenerateRunTransition(BoundRunStatementNode runStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(edges.Count == 1);

        writer.Write("fields.reference");
        writer.Write(runStatement.Reference.Name);
        writer.Write('.');
        writer.Write(runStatement.Method.Name);

        writer.Write('(');

        if (runStatement.Arguments.Length > 0)
        {
            GeneralEmission.GenerateExpression(runStatement.Arguments[0].Expression, writer);

            foreach (BoundArgumentNode argument in runStatement.Arguments.Skip(1))
            {
                writer.Write(", ");
                GeneralEmission.GenerateExpression(argument.Expression, writer);
            }
        }

        writer.WriteLine(");");

        GenerateTransitionTo(edges.First(e => e.IsStory).ToVertex);
    }

    private void GenerateChooseTransition(BoundChooseStatementNode chooseStatement, ImmutableList<FlowEdge> edges)
    {
        writer.BeginBlock();

        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine("[] options = optionsPool.Value!;");

        for (int i = 0; i < chooseStatement.Options.Length; i++)
        {
            writer.Write("options[");
            writer.Write(i);
            writer.Write("] = ");
            GeneralEmission.GenerateExpression(chooseStatement.Options[i].Expression, writer);
            writer.WriteLine(';');
        }

        writer.WriteLine();

        writer.Write("int next = fields.reference");
        writer.Write(chooseStatement.Reference.Name);
        writer.Write('.');
        writer.Write(chooseStatement.Method.Name);

        writer.Write('(');

        foreach (BoundArgumentNode argument in chooseStatement.Arguments)
        {
            GeneralEmission.GenerateExpression(argument.Expression, writer);
            writer.Write(", ");
        }

        writer.Write("new global::Phantonia.Historia.ReadOnlyList<");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.Write(">(options, 0, ");
        writer.Write(chooseStatement.Options.Length);
        writer.WriteLine("));");

        writer.WriteLine();

        writer.WriteLine("switch (next)");
        writer.BeginBlock();

        for (int i = 0; i < chooseStatement.Options.Length; i++)
        {
            writer.Write("case ");
            writer.Write(i);
            writer.WriteLine(':');
            writer.Indent++;

            GenerateTransitionTo(edges[i].ToVertex);

            writer.Indent--;
        }

        writer.WriteLine("default:");
        writer.Indent++;

        writer.WriteLine($$"""throw new global::System.InvalidOperationException($"Expected an option between 0 and {{chooseStatement.Options.Length - 1}}, instead got option {next}");""");

        writer.Indent--;

        writer.EndBlock(); // switch

        writer.EndBlock(); // scope
    }

    private void GenerateIfTransition(IfStatementNode ifStatement, ImmutableList<FlowEdge> edges)
    {
        Debug.Assert(edges.Count == 2);

        writer.Write("if (");
        GeneralEmission.GenerateExpression(ifStatement.Condition, writer);
        writer.WriteLine(")");

        writer.BeginBlock();
        GenerateTransitionTo(edges[0].ToVertex);
        writer.EndBlock();

        writer.WriteLine("else");

        writer.BeginBlock();
        GenerateTransitionTo(edges[1].ToVertex);
        writer.EndBlock();
    }

    private void GenerateTransitionTo(int toVertex)
    {
        void GenerateSimpleTransitionTo(int toVertex)
        {
            writer.WriteLine($"fields.state = {toVertex};");

            if (toVertex == Constants.EndState || flowGraph.Vertices[toVertex].IsVisible)
            {
                writer.WriteLine("return;");
            }
            else
            {
                writer.WriteLine("continue;");
            }
        }

        if (toVertex != Constants.EndState
            && flowGraph.Vertices[toVertex].AssociatedStatement is FlowBranchingStatementNode
            {
                Original: LoopSwitchStatementNode loopSwitch,
                OutgoingEdges: [.., FlowEdge lastEdge],
            }
            && loopSwitch.Options.All(o => o.Kind != LoopSwitchOptionKind.Final))
        {
            // this loop switch has no final option, that is it terminates after all normal options have been selected
            writer.Write("if (fields.");
            GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            writer.Write(" == ");
            writer.Write((1 << loopSwitch.Options.Length) - 1); // (ls == 2^(options.length) - 1) means all bits for options have been set
            writer.WriteLine(')');

            writer.BeginBlock();

            // reset loop switch field
            writer.Write("fields.");
            GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            writer.WriteLine(" = 0;");

            // the last edge of these loop switches is the one to the next state
            GenerateSimpleTransitionTo(lastEdge.ToVertex);

            writer.EndBlock(); // if

            writer.WriteLine("else");

            writer.BeginBlock();
            GenerateSimpleTransitionTo(toVertex);
            writer.EndBlock(); // else
        }
        else
        {
            GenerateSimpleTransitionTo(toVertex);
        }
    }
}
