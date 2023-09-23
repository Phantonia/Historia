using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StatementBodyNode) BindStatementBody(StatementBodyNode body, Settings settings, SymbolTable table)
    {
        table = table.OpenScope();

        List<StatementNode> statements = body.Statements.ToList();

        for (int i = 0; i < statements.Count; i++)
        {
            StatementNode statement = statements[i];
            (table, StatementNode boundStatement) = BindStatement(statement, settings, table);
            statements[i] = boundStatement;
        }

        table = table.CloseScope();

        return (table, body with
        {
            Statements = statements.ToImmutableArray(),
        });
    }

    private (SymbolTable, StatementNode) BindStatement(StatementNode statement, Settings settings, SymbolTable table)
    {
        switch (statement)
        {
            case OutputStatementNode outputStatement:
                {
                    (table, ExpressionNode boundExpression) = BindAndTypeExpression(outputStatement.OutputExpression, table);

                    if (boundExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
                    {
                        if (!TypesAreCompatible(sourceType, settings.OutputType))
                        {
                            ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", boundExpression.Index));

                            return (table, statement);
                        }

                        boundExpression = typedExpression with
                        {
                            TargetType = settings.OutputType,
                        };
                    }

                    OutputStatementNode boundStatement = outputStatement with
                    {
                        OutputExpression = boundExpression,
                    };

                    return (table, boundStatement);
                }
            case SwitchStatementNode switchStatement:
                return BindSwitchStatement(switchStatement, settings, table);
            case LoopSwitchStatementNode loopSwitchStatement:
                return BindLoopSwitchStatement(loopSwitchStatement, settings, table);
            case OutcomeDeclarationStatementNode outcomeDeclaration:
                return BindOutcomeDeclarationStatement(outcomeDeclaration, table);
            case SpectrumDeclarationStatementNode spectrumDeclaration:
                return BindSpectrumDeclarationStatement(spectrumDeclaration, table);
            case BranchOnStatementNode branchOnStatement:
                return BindBranchOnStatement(branchOnStatement, settings, table);
            case AssignmentStatementNode assignmentStatement:
                return BindAssignmentStatement(assignmentStatement, table);
            case SpectrumAdjustmentStatementNode adjustmentStatement:
                return BindSpectrumAdjustmentStatement(adjustmentStatement, table);
            case CallStatementNode callStatement:
                return BindCallStatement(callStatement, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, StatementNode) BindSwitchStatement(SwitchStatementNode switchStatement, Settings settings, SymbolTable table)
    {
        (table, ExpressionNode outputExpression) = BindAndTypeExpression(switchStatement.OutputExpression, table);

        {
            if (outputExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OutputType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", outputExpression.Index));
                }
                else
                {
                    outputExpression = typedExpression with
                    {
                        TargetType = settings.OutputType,
                    };
                }
            }
        }

        if (switchStatement.Name is not null)
        {
            if (switchStatement.Options.Any(o => o.Name is null))
            {
                ErrorFound?.Invoke(Errors.InconsistentNamedSwitch(switchStatement.Index));
            }
            else if (table.IsDeclared(switchStatement.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedSymbolName(switchStatement.Name, switchStatement.Index));
            }
            else
            {
                HashSet<string> optionNames = new();

                foreach (SwitchOptionNode option in switchStatement.Options)
                {
                    if (!optionNames.Add(option.Name!))
                    {
                        ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option.Name!, option.Index));
                    }
                }

                OutcomeSymbol symbol = new()
                {
                    Name = switchStatement.Name,
                    OptionNames = optionNames.ToImmutableArray(),
                    AlwaysAssigned = true,
                    Index = switchStatement.Index,
                };

                table = table.Declare(symbol);
            }
        }
        else
        {
            if (switchStatement.Options.Any(o => o.Name is not null))
            {
                ErrorFound?.Invoke(Errors.InconsistentUnnamedSwitch(switchStatement.Index));
            }
        }

        List<SwitchOptionNode> boundOptions = switchStatement.Options.ToList();

        for (int i = 0; i < boundOptions.Count; i++)
        {
            (table, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, table);

            if (optionExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OptionType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OptionType, "option", optionExpression.Index));
                }
                else
                {
                    optionExpression = typedExpression with
                    {
                        TargetType = settings.OptionType,
                    };
                }
            }

            (table, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, settings, table);

            boundOptions[i] = boundOptions[i] with
            {
                Expression = optionExpression,
                Body = optionBody,
            };
        }

        SwitchStatementNode boundStatement;

        if (switchStatement.Name is not null && table.IsDeclared(switchStatement.Name) && table[switchStatement.Name] is OutcomeSymbol)
        {
            OutcomeSymbol outcome = (OutcomeSymbol)table[switchStatement.Name];

            boundStatement = new BoundNamedSwitchStatementNode
            {
                Name = switchStatement.Name,
                OutputExpression = outputExpression,
                Options = boundOptions.ToImmutableArray(),
                Outcome = outcome,
                Index = switchStatement.Index,
            };
        }
        else
        {
            boundStatement = switchStatement with
            {
                OutputExpression = outputExpression,
                Options = boundOptions.ToImmutableArray(),
            };
        }

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindLoopSwitchStatement(LoopSwitchStatementNode loopSwitchStatement, Settings settings, SymbolTable table)
    {
        (table, ExpressionNode outputExpression) = BindAndTypeExpression(loopSwitchStatement.OutputExpression, table);

        {
            if (outputExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OutputType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", outputExpression.Index));
                }
                else
                {
                    outputExpression = typedExpression with
                    {
                        TargetType = settings.OutputType,
                    };
                }
            }
        }

        List<LoopSwitchOptionNode> boundOptions = loopSwitchStatement.Options.ToList();

        for (int i = 0; i < boundOptions.Count; i++)
        {
            (table, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, table);

            if (optionExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OptionType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OptionType, "option", optionExpression.Index));
                }
                else
                {
                    optionExpression = typedExpression with
                    {
                        TargetType = settings.OptionType,
                    };
                }
            }

            (table, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, settings, table);

            boundOptions[i] = boundOptions[i] with
            {
                Expression = optionExpression,
                Body = optionBody,
            };
        }

        // [not yet in spec]: A looped switch terminates if one of two conditions is met:
        // - a final option has been selected
        // - there are no options left, as all normal options have already been selected
        // A looped switches has to be able to terminate, that is, if it contains a looped option,
        // it also has to have a final option.

        // !(hasLoopedOption => hasFinalOption) === hasLoopedOption && !hasFinalOption
        if (boundOptions.Any(o => o.Kind == LoopSwitchOptionKind.Loop) && boundOptions.All(o => o.Kind != LoopSwitchOptionKind.Final))
        {
            ErrorFound?.Invoke(Errors.LoopSwitchHasToTerminate(loopSwitchStatement.Index));
        }

        LoopSwitchStatementNode boundStatement;

        boundStatement = loopSwitchStatement with
        {
            OutputExpression = outputExpression,
            Options = boundOptions.ToImmutableArray(),
        };

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindOutcomeDeclarationStatement(OutcomeDeclarationStatementNode outcomeDeclaration, SymbolTable table)
    {
        (table, OutcomeSymbol? symbol) = BindOutcomeDeclaration(outcomeDeclaration, table);

        if (symbol is null)
        {
            return (table, outcomeDeclaration);
        }

        BoundOutcomeDeclarationStatementNode boundStatement = new()
        {
            Name = outcomeDeclaration.Name,
            Options = outcomeDeclaration.Options,
            DefaultOption = outcomeDeclaration.DefaultOption,
            Index = outcomeDeclaration.Index,
            Outcome = symbol,
        };

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindSpectrumDeclarationStatement(SpectrumDeclarationStatementNode spectrumDeclaration, SymbolTable table)
    {
        (table, SpectrumSymbol? symbol) = BindSpectrumDeclaration(spectrumDeclaration, table);

        if (symbol is null)
        {
            return (table, spectrumDeclaration);
        }

        BoundSpectrumDeclarationStatementNode boundStatement = new()
        {
            Name = spectrumDeclaration.Name,
            Options = spectrumDeclaration.Options,
            DefaultOption = spectrumDeclaration.DefaultOption,
            Index = spectrumDeclaration.Index,
            Spectrum = symbol,
        };

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindAssignmentStatement(AssignmentStatementNode assignmentStatement, SymbolTable table)
    {
        if (!table.IsDeclared(assignmentStatement.VariableName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(assignmentStatement.VariableName, assignmentStatement.Index));
            return (table, assignmentStatement);
        }

        Symbol symbol = table[assignmentStatement.VariableName];

        switch (symbol)
        {
            case OutcomeSymbol outcomeSymbol and not SpectrumSymbol: // spectrums are outcomes but cannot be assigned to
                {
                    if (assignmentStatement.AssignedExpression is not IdentifierExpressionNode { Identifier: string option })
                    {
                        ErrorFound?.Invoke(Errors.OutcomeAssignedNonIdentifier(assignmentStatement.VariableName, assignmentStatement.AssignedExpression.Index));
                        return (table, assignmentStatement);
                    }

                    if (!outcomeSymbol.OptionNames.Contains(option))
                    {
                        ErrorFound?.Invoke(Errors.OptionDoesNotExistInOutcome(assignmentStatement.VariableName, option, assignmentStatement.AssignedExpression.Index));
                        return (table, assignmentStatement);
                    }

                    BoundOutcomeAssignmentStatementNode boundAssignment = new()
                    {
                        VariableName = assignmentStatement.VariableName,
                        AssignedExpression = assignmentStatement.AssignedExpression,
                        Index = assignmentStatement.Index,
                        Outcome = outcomeSymbol,
                    };

                    return (table, boundAssignment);
                }
            default:
                ErrorFound?.Invoke(Errors.SymbolCannotBeAssignedTo(symbol.Name, assignmentStatement.Index));
                return (table, assignmentStatement);
        }
    }

    private (SymbolTable, StatementNode) BindSpectrumAdjustmentStatement(SpectrumAdjustmentStatementNode adjustmentStatement, SymbolTable table)
    {
        if (!table.IsDeclared(adjustmentStatement.SpectrumName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(adjustmentStatement.SpectrumName, adjustmentStatement.Index));
            return (table, adjustmentStatement);
        }

        Symbol symbol = table[adjustmentStatement.SpectrumName];

        if (symbol is not SpectrumSymbol spectrumSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotSpectrum(adjustmentStatement.SpectrumName, adjustmentStatement.Index));
            return (table, adjustmentStatement);
        }

        (table, ExpressionNode amount) = BindAndTypeExpression(adjustmentStatement.AdjustmentAmount, table);

        if (amount is not TypedExpressionNode typedAmount)
        {
            return (table, adjustmentStatement);
        }

        if (!TypesAreCompatible(typedAmount.SourceType, (TypeSymbol)table["Int"]))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(typedAmount.SourceType, (TypeSymbol)table["Int"], "strengthen/weaken amount", typedAmount.Index));
        }

        typedAmount = typedAmount with
        {
            TargetType = (TypeSymbol)table["Int"],
        };

        BoundSpectrumAdjustmentStatementNode boundStatement = new()
        {
            Spectrum = spectrumSymbol,
            Strengthens = adjustmentStatement.Strengthens,
            SpectrumName = adjustmentStatement.SpectrumName,
            AdjustmentAmount = typedAmount,
            Index = adjustmentStatement.Index,
        };

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindBranchOnStatement(BranchOnStatementNode branchOnStatement, Settings settings, SymbolTable table)
    {
        if (!table.IsDeclared(branchOnStatement.OutcomeName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(branchOnStatement.OutcomeName, branchOnStatement.Index));
            return (table, branchOnStatement);
        }

        if (table[branchOnStatement.OutcomeName] is not OutcomeSymbol outcomeSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(branchOnStatement.OutcomeName, branchOnStatement.Index));
            return (table, branchOnStatement);
        }

        HashSet<string> uniqueOptionNames = new();
        List<BranchOnOptionNode> boundOptions = new();

        bool errorWithOptions = false;

        foreach (BranchOnOptionNode option in branchOnStatement.Options)
        {
            bool error = false;

            if (option is NamedBranchOnOptionNode { OptionName: string optionName })
            {
                if (!outcomeSymbol.OptionNames.Contains(optionName))
                {
                    ErrorFound?.Invoke(Errors.OptionDoesNotExistInOutcome(outcomeSymbol.Name, optionName, option.Index));
                    error = true;
                    errorWithOptions = true;
                }
                else if (!uniqueOptionNames.Add(optionName))
                {
                    ErrorFound?.Invoke(Errors.BranchOnDuplicateOption(outcomeSymbol.Name, optionName, option.Index));
                    error = true;
                    errorWithOptions = true;
                }
            }

            (table, StatementBodyNode boundBody) = BindStatementBody(option.Body, settings, table);

            if (!error)
            {
                boundOptions.Add(option with { Body = boundBody });
            }
        }

        if (!errorWithOptions)
        {
            if (branchOnStatement.Options.Length == 0 || (branchOnStatement.Options.Length < outcomeSymbol.OptionNames.Length && branchOnStatement.Options[^1] is not OtherBranchOnOptionNode))
            {
                IEnumerable<string> missingOptionNames = outcomeSymbol.OptionNames.Except(branchOnStatement.Options.OfType<NamedBranchOnOptionNode>().Select(o => o.OptionName));
                ErrorFound?.Invoke(Errors.BranchOnIsNotExhaustive(outcomeSymbol.Name, missingOptionNames, branchOnStatement.Index));
            }
            else if (branchOnStatement.Options.Length == uniqueOptionNames.Count + 1 && branchOnStatement.Options.Length == outcomeSymbol.OptionNames.Length + 1)
            {
                // this should only be able to happen if we cover every option and also have an other branch
                Debug.Assert(branchOnStatement.Options[^1] is OtherBranchOnOptionNode);

                ErrorFound?.Invoke(Errors.BranchOnIsExhaustiveAndHasOtherBranch(outcomeSymbol.Name, branchOnStatement.Index));
            }
        }

        BoundBranchOnStatementNode boundStatement = new()
        {
            Outcome = outcomeSymbol,
            OutcomeName = outcomeSymbol.Name,
            Options = boundOptions.ToImmutableArray(),
            Index = branchOnStatement.Index,
        };

        return (table, boundStatement);
    }

    private (SymbolTable, StatementNode) BindCallStatement(CallStatementNode callStatement, SymbolTable table)
    {
        if (!table.IsDeclared(callStatement.SceneName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(callStatement.SceneName, callStatement.Index));
            return (table, callStatement);
        }

        if (table[callStatement.SceneName] is not SceneSymbol sceneSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(callStatement.SceneName, callStatement.Index));
            return (table, callStatement);
        }

        BoundCallStatementNode boundCallStatement = new()
        {
            Scene = sceneSymbol,
            SceneName = callStatement.SceneName,
            Index = callStatement.Index,
        };

        return (table, boundCallStatement);
    }
}
