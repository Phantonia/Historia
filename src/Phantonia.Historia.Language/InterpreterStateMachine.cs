using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language;

public sealed class InterpreterStateMachine : IStory
{
    private const int EndState = FlowGraph.FinalVertex;
    private const int StartState = -2;

    public InterpreterStateMachine(FlowGraph flowGraph, SymbolTable symbolTable)
    {
        this.flowGraph = flowGraph;
        this.symbolTable = symbolTable;
    }

    private readonly FlowGraph flowGraph;
    private readonly SymbolTable symbolTable;

    private int state = StartState;
    private readonly Dictionary<string, ulong> fields = new();
    private readonly List<object> options = new();

    public bool NotStartedStory { get; private set; } = true;

    public bool FinishedStory { get; private set; } = false;

    public object? Output { get; private set; }

    public ReadOnlyList<object> Options => new(options, 0, options.Count);

    public bool TryContinue()
    {
        if (FinishedStory || Options.Count != 0)
        {
            return false;
        }

        StateTransition(0);
        SetOutput();
        SetOptions();

        if (state != StartState)
        {
            NotStartedStory = false;
        }

        if (state == EndState)
        {
            FinishedStory = true;
        }

        return true;
    }

    public bool TryContinueWithOption(int option)
    {
        if (FinishedStory || option < 0 || option >= Options.Count)
        {
            return false;
        }

        StateTransition(option);
        SetOutput();
        SetOptions();

        if (state != StartState)
        {
            NotStartedStory = false;
        }

        if (state == EndState)
        {
            FinishedStory = true;
        }

        return true;
    }

    public void Reset()
    {
        NotStartedStory = true;
        FinishedStory = false;
        fields.Clear();
        options.Clear();
        state = StartState;
    }

    private void StateTransition(int option)
    {
        while (true)
        {
            if (state == StartState)
            {
                state = flowGraph.StartVertex;

                if (flowGraph.Vertices[state].IsVisible)
                {
                    return;
                }
            }

            switch (flowGraph.Vertices[state].AssociatedStatement)
            {
                case OutputStatementNode:
                    state = flowGraph.OutgoingEdges[state][0].ToVertex;
                    break;
                case SwitchStatementNode:
                    state = flowGraph.OutgoingEdges[state][option].ToVertex;
                    break;
                case LoopSwitchStatementNode loopSwitchStatement:
                    {
                        string fieldName = $"ls{loopSwitchStatement.Index}";
                        fields.TryAdd(fieldName, 0);

                        int tempOption = option;
                        int realOption = 0;

                        for (int i = 0; i < 64; i++)
                        {
                            if ((fields[fieldName] & (1UL << i)) == 0)
                            {
                                tempOption--;
                            }

                            if (tempOption < 0)
                            {
                                break;
                            }

                            realOption++;
                        }

                        switch (loopSwitchStatement.Options[realOption].Kind)
                        {
                            case LoopSwitchOptionKind.None:
                                fields[fieldName] |= 1UL << realOption;
                                break;
                            case LoopSwitchOptionKind.Final:
                                fields[fieldName] = 0;
                                break;
                        }

                        state = flowGraph.OutgoingEdges[state][realOption].ToVertex;
                    }
                    break;
                case BoundBranchOnStatementNode { Outcome: SpectrumSymbol spectrum } branchOnStatement:
                    SpectrumBranchOnTransition(branchOnStatement, spectrum);
                    break;
                case BoundBranchOnStatementNode { Outcome: OutcomeSymbol outcome } branchOnStatement:
                    OutcomeBranchOnTransition(branchOnStatement, outcome);
                    break;
                case BoundOutcomeAssignmentStatementNode { Outcome: OutcomeSymbol outcome, AssignedOption: string assignedOption }:
                    {
                        string fieldName = $"outcome{outcome.Index}";

                        int index = outcome.OptionNames.IndexOf(assignedOption);
                        fields[fieldName] = (ulong)index;

                        Debug.Assert(flowGraph.OutgoingEdges[state].Count == 1);
                        state = flowGraph.OutgoingEdges[state][0].ToVertex;
                    }
                    break;
                case BoundSpectrumAdjustmentStatementNode
                {
                    Spectrum: SpectrumSymbol spectrum,
                    Strengthens: bool strengthens,
                    AdjustmentAmount: TypedExpressionNode
                    {
                        Expression: IntegerLiteralExpressionNode { Value: int amount },
                    },
                }:
                    {
                        string totalFieldName = $"total{spectrum.Index}";
                        fields.TryAdd(totalFieldName, 0);
                        fields[totalFieldName] += (ulong)amount;

                        if (strengthens)
                        {
                            string positiveFieldName = $"positive{spectrum.Index}";
                            fields.TryAdd(positiveFieldName, 0);
                            fields[positiveFieldName] += (ulong)amount;
                        }

                        state = flowGraph.OutgoingEdges[state][0].ToVertex;
                    }
                    break;
                case CallerTrackerStatementNode callerTracker:
                    {
                        string fieldName = $"tracker{callerTracker.Tracker.Index}";
                        fields[fieldName] = (ulong)callerTracker.CallSiteIndex;
                        state = flowGraph.OutgoingEdges[state][0].ToVertex;
                    }
                    break;
                case CallerResolutionStatementNode callerResolution:
                    {
                        string fieldName = $"tracker{callerResolution.Tracker.Index}";
                        int callSite = (int)fields[fieldName];
                        state = flowGraph.OutgoingEdges[state][callSite].ToVertex;
                    }
                    break;
            }

            if (state == EndState || flowGraph.Vertices[state].IsVisible)
            {
                return;
            }
            else
            {
                continue;
            }
        }
    }

    private void SpectrumBranchOnTransition(BranchOnStatementNode branchOnStatement, SpectrumSymbol spectrum)
    {
        Debug.Assert(spectrum.Intervals.All(i => i.Value.UpperDenominator == spectrum.Intervals.First().Value.UpperDenominator));

        string totalFieldName = $"total{spectrum.Index}";
        string positiveFieldName = $"positive{spectrum.Index}";

        fields.TryAdd(totalFieldName, 0);
        fields.TryAdd(positiveFieldName, 0);

        if (spectrum.DefaultOption is not null && fields[totalFieldName] == 0)
        {
            state = flowGraph.OutgoingEdges[state][spectrum.OptionNames.IndexOf(spectrum.DefaultOption)].ToVertex;
            return;
        }

        int value = (int)fields[positiveFieldName] * spectrum.Intervals.First().Value.UpperDenominator;

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
            BranchOnOptionNode branchOnOption = options[i];

            Debug.Assert(branchOnOption is NamedBranchOnOptionNode); // only the last option may not be named
            string optionName = ((NamedBranchOnOptionNode)branchOnOption).OptionName;

            SpectrumInterval interval = spectrum.Intervals[optionName];

            if (interval.Inclusive
                ? value <= (int)fields[totalFieldName] * interval.UpperNumerator
                : value < (int)fields[totalFieldName] * interval.UpperNumerator)
            {
                int index = branchOnStatement.Options.IndexOf(branchOnOption);
                state = flowGraph.OutgoingEdges[state][index].ToVertex;
                return;
            }
        }

        {
            // other
            int index = branchOnStatement.Options.IndexOf(options[^1]);
            state = flowGraph.OutgoingEdges[state][index].ToVertex;
        }
    }

    private void OutcomeBranchOnTransition(BoundBranchOnStatementNode branchOnStatement, OutcomeSymbol outcome)
    {
        string fieldName = $"outcome{outcome.Index}";

        int actualOutcome = (int)fields[fieldName];
        int index = -1;

        for (int i = 0; i < branchOnStatement.Options.Length; i++)
        {
            if (branchOnStatement.Options[i] is NamedBranchOnOptionNode { OptionName: string name }
                && name == outcome.OptionNames[actualOutcome])
            {
                index = i;
                break;
            }
        }

        state = flowGraph.OutgoingEdges[state][index].ToVertex;
    }

    private void SetOutput()
    {
        if (state == EndState)
        {
            Output = null;
            return;
        }

        IOutputStatementNode outputState = (IOutputStatementNode)flowGraph.Vertices[state].AssociatedStatement;

        Output = ExpressionToObject(outputState.OutputExpression);
    }

    private void SetOptions()
    {
        if (state == EndState)
        {
            options.Clear();
            return;
        }

        StatementNode statement = flowGraph.Vertices[state].AssociatedStatement;

        options.Clear();

        switch (statement)
        {
            case SwitchStatementNode switchStatement:
                options.AddRange(switchStatement.Options.Select(option => ExpressionToObject(option.Expression)));
                return;
            case LoopSwitchStatementNode loopSwitchStatement:
                {
                    string fieldName = $"ls{loopSwitchStatement.Index}";
                    fields.TryAdd(fieldName, 0);
                    ulong mask = fields[fieldName];

                    for (int i = 0; i < loopSwitchStatement.Options.Length; i++)
                    {
                        LoopSwitchOptionNode option = loopSwitchStatement.Options[i];

                        if ((mask & (1UL << i)) == 0)
                        {
                            options.Add(ExpressionToObject(option.Expression));
                        }
                    }
                }
                return;
        }
    }

    private static object ExpressionToObject(ExpressionNode expression)
    {
        Debug.Assert(expression is TypedExpressionNode);

        expression = ((TypedExpressionNode)expression).Expression;

        switch (expression)
        {
            case StringLiteralExpressionNode { StringLiteral: string literal }:
                return literal;
            case IntegerLiteralExpressionNode { Value: int literal }:
                return literal;
            case BoundEnumOptionExpressionNode { EnumSymbol.Name: string enumName, OptionName: string optionName }:
                return $"{enumName}.{optionName}";
            case BoundRecordCreationExpressionNode { Record.Name: string recordName, BoundArguments: ImmutableArray<BoundArgumentNode> arguments }:
                {
                    ImmutableDictionary<string, object> properties
                        = arguments.Select(a => new KeyValuePair<string, object>(a.Property.Name, ExpressionToObject(a.Expression)))
                                   .ToImmutableDictionary();
                    return new RecordInstance
                    {
                        RecordName = recordName,
                        Properties = properties,
                    };
                }
            default:
                throw new InvalidOperationException();
        }
    }

    IReadOnlyList<object?> IStory.Options => Options;
}
