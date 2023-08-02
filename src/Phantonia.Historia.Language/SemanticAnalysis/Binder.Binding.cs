﻿using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StoryNode) BindPseudoSymbols(SymbolTable table)
    {
        StoryNode boundStory = story;

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
            if (declaration is not TypeSymbolDeclarationNode)
            {
                continue;
            }

            (table, TopLevelNode boundDeclaration) = BindPseudoDeclaration(declaration, table);
            boundStory = boundStory with
            {
                TopLevelNodes = boundStory.TopLevelNodes.Replace(declaration, boundDeclaration),
            };
        }

        return (table, boundStory);
    }

    private (SymbolTable, TopLevelNode) BindPseudoDeclaration(TopLevelNode declaration, SymbolTable table)
    {
        switch (declaration)
        {
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindPseudoRecordDeclaration(recordDeclaration, table);
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                return BindPseudoUnionDeclaration(unionDeclaration, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, TopLevelNode) BindPseudoRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
    {
        List<PropertyDeclarationNode> properties = recordDeclaration.Properties.ToList();

        for (int i = 0; i < properties.Count; i++)
        {
            (table, TypeNode boundType) = BindType(properties[i].Type, table);
            properties[i] = properties[i] with
            {
                Type = boundType,
            };
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = recordDeclaration.Name,
            Declaration = recordDeclaration with
            {
                Properties = properties.ToImmutableArray(),
            },
            Symbol = table[recordDeclaration.Name],
            Index = recordDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindPseudoUnionDeclaration(UnionTypeSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        List<TypeNode> subtypes = unionDeclaration.Subtypes.ToList();

        for (int i = 0; i < subtypes.Count; i++)
        {
            (table, TypeNode boundType) = BindType(subtypes[i], table);
            subtypes[i] = boundType;
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Declaration = unionDeclaration with
            {
                Subtypes = subtypes.ToImmutableArray(),
            },
            Symbol = table[unionDeclaration.Name],
            Index = unionDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, Settings, StoryNode) BindSettingDirectives(StoryNode halfboundStory, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = halfboundStory.TopLevelNodes.ToList();

        for (int i = 0; i < topLevelNodes.Count; i++)
        {
            TopLevelNode topLevelNode = topLevelNodes[i];

            if (topLevelNode is not SettingDirectiveNode directive)
            {
                // already bound these
                continue;
            }

            (table, SettingDirectiveNode boundDirective) = BindSingleSettingDirective(directive, table);
            topLevelNodes[i] = boundDirective;
        }

        halfboundStory = halfboundStory with { TopLevelNodes = topLevelNodes.ToImmutableArray() };

        Settings settings = new();

        foreach (SettingDirectiveNode directive in topLevelNodes.OfType<SettingDirectiveNode>())
        {
            switch (directive)
            {
                case TypeSettingDirectiveNode
                {
                    SettingName: nameof(Settings.OutputType),
                    Type: BoundTypeNode { Symbol: TypeSymbol outputType }
                }:
                    settings = settings with { OutputType = outputType };
                    break;
                case TypeSettingDirectiveNode
                {
                    SettingName: nameof(Settings.OptionType),
                    Type: BoundTypeNode { Symbol: TypeSymbol optionType }
                }:
                    settings = settings with { OptionType = optionType };
                    break;
            }
        }

        return (table, settings, halfboundStory);
    }

    private (SymbolTable, StoryNode) BindTree(StoryNode halfboundStory, Settings settings, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = halfboundStory.TopLevelNodes.ToList();

        for (int i = 0; i < topLevelNodes.Count; i++)
        {
            TopLevelNode topLevelNode = topLevelNodes[i];

            if (topLevelNode is TypeSymbolDeclarationNode or SettingDirectiveNode)
            {
                // already bound these
                continue;
            }

            (table, TopLevelNode boundDeclaration) = BindTopLevelNode(topLevelNode, settings, table);
            topLevelNodes[i] = boundDeclaration;
        }

        StoryNode boundStory = halfboundStory with { TopLevelNodes = topLevelNodes.ToImmutableArray() };
        return (table, boundStory);
    }

    private (SymbolTable, TopLevelNode) BindTopLevelNode(TopLevelNode declaration, Settings settings, SymbolTable table)
    {
        if (declaration is BoundSymbolDeclarationNode { Declaration: var innerDeclaration })
        {
            return BindTopLevelNode(innerDeclaration, settings, table);
        }

        switch (declaration)
        {
            case SceneSymbolDeclarationNode sceneDeclaration:
                return BindSceneDeclaration(sceneDeclaration, settings, table);
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindRecordDeclaration(recordDeclaration, table);
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                return BindUnionDeclaration(unionDeclaration, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private static (SymbolTable, BoundSymbolDeclarationNode) BindRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(recordDeclaration.Name) && table[recordDeclaration.Name] is RecordTypeSymbol);

        RecordTypeSymbol recordSymbol = (RecordTypeSymbol)table[recordDeclaration.Name];

        ImmutableArray<PropertyDeclarationNode>.Builder boundPropertyDeclarations = ImmutableArray.CreateBuilder<PropertyDeclarationNode>(recordDeclaration.Properties.Length);

        foreach ((PropertyDeclarationNode propertyDeclaration, PropertySymbol propertySymbol) in recordDeclaration.Properties.Zip(recordSymbol.Properties))
        {
            Debug.Assert(propertyDeclaration.Name == propertySymbol.Name);
            Debug.Assert(propertyDeclaration.Index == propertySymbol.Index);

            boundPropertyDeclarations.Add(new BoundPropertyDeclarationNode
            {
                Name = propertySymbol.Name,
                Symbol = propertySymbol,
                Type = propertyDeclaration.Type,
                Index = propertySymbol.Index,
            });
        }

        BoundSymbolDeclarationNode boundRecordDeclaration = new()
        {
            Name = recordDeclaration.Name,
            Declaration = recordDeclaration with
            {
                Properties = boundPropertyDeclarations.MoveToImmutable(),
            },
            Symbol = recordSymbol,
            Index = recordDeclaration.Index,
        };

        return (table, boundRecordDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindUnionDeclaration(UnionTypeSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(unionDeclaration.Name) && table[unionDeclaration.Name] is UnionTypeSymbol);

        UnionTypeSymbol unionSymbol = (UnionTypeSymbol)table[unionDeclaration.Name];

        BoundSymbolDeclarationNode boundUnionDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Declaration = unionDeclaration,
            Symbol = unionSymbol,
            Index = unionDeclaration.Index,
        };

        return (table, boundUnionDeclaration);
    }

    private (SymbolTable, SettingDirectiveNode) BindSingleSettingDirective(SettingDirectiveNode directive, SymbolTable table)
    {
        switch (directive)
        {
            case TypeSettingDirectiveNode typeSetting:
                (table, TypeNode boundType) = BindType(typeSetting.Type, table);
                return (table, typeSetting with { Type = boundType });
            case ExpressionSettingDirectiveNode expressionSetting:
                (table, ExpressionNode boundExpression) = BindAndTypeExpression(expressionSetting.Expression, table);
                return (table, expressionSetting with { Expression = boundExpression });
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, TypeNode) BindType(TypeNode type, SymbolTable table)
    {
        switch (type)
        {
            case IdentifierTypeNode identifierType:
                {
                    if (!table.IsDeclared(identifierType.Identifier))
                    {
                        ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(identifierType.Identifier, identifierType.Index));
                        return (table, type);
                    }

                    Symbol symbol = table[identifierType.Identifier];

                    if (symbol is not TypeSymbol typeSymbol)
                    {
                        ErrorFound?.Invoke(Errors.NonTypeSymbolUsedAsType(identifierType.Identifier, identifierType.Index));
                        return (table, type);
                    }

                    BoundTypeNode boundType = new() { Node = type, Symbol = typeSymbol, Index = type.Index };
                    return (table, boundType);
                }
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, BoundSymbolDeclarationNode) BindSceneDeclaration(SceneSymbolDeclarationNode sceneDeclaration, Settings settings, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(sceneDeclaration.Name) && table[sceneDeclaration.Name] is SceneSymbol);

        SceneSymbol sceneSymbol = (SceneSymbol)table[sceneDeclaration.Name];

        (table, StatementBodyNode boundBody) = BindStatementBody(sceneDeclaration.Body, settings, table);

        BoundSymbolDeclarationNode boundSceneDeclaration = new()
        {
            Declaration = sceneDeclaration with
            {
                Body = boundBody,
            },
            Symbol = sceneSymbol,
            Name = sceneDeclaration.Name,
            Index = sceneDeclaration.Index,
        };

        return (table, boundSceneDeclaration);
    }

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

                    return (table, switchStatement);
                }

                outputExpression = typedExpression with
                {
                    TargetType = settings.OutputType,
                };
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

                    return (table, switchStatement);
                }

                optionExpression = typedExpression with
                {
                    TargetType = settings.OptionType,
                };
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

    private (SymbolTable, OutcomeSymbol?) BindOutcomeDeclaration(IOutcomeDeclarationNode outcomeDeclaration, SymbolTable table)
    {
        bool error = false;

        if (table.IsDeclared(outcomeDeclaration.Name))
        {
            ErrorFound?.Invoke(Errors.DuplicatedSymbolName(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        if (outcomeDeclaration.Options.Length == 0)
        {
            ErrorFound?.Invoke(Errors.OutcomeWithZeroOptions(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        HashSet<string> optionNames = new();

        foreach (string option in outcomeDeclaration.Options)
        {
            if (!optionNames.Add(option))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option, outcomeDeclaration.Index));
                error = true;
            }
        }

        if (outcomeDeclaration.DefaultOption is not null && !optionNames.Contains(outcomeDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        if (error)
        {
            return (table, null);
        }

        OutcomeSymbol symbol = new()
        {
            Name = outcomeDeclaration.Name,
            OptionNames = optionNames.ToImmutableArray(),
            DefaultOption = outcomeDeclaration.DefaultOption,
            Index = outcomeDeclaration.Index,
        };

        table = table.Declare(symbol);

        return (table, symbol);
    }

    private (SymbolTable, SpectrumSymbol?) BindSpectrumDeclaration(ISpectrumDeclarationNode spectrumDeclaration, SymbolTable table)
    {
        bool error = false;

        if (table.IsDeclared(spectrumDeclaration.Name))
        {
            ErrorFound?.Invoke(Errors.DuplicatedSymbolName(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        if (spectrumDeclaration.Options.Length == 0)
        {
            ErrorFound?.Invoke(Errors.OutcomeWithZeroOptions(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        HashSet<string> optionNames = new();

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            if (!optionNames.Add(option.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option.Name, spectrumDeclaration.Index));
                error = true;
            }

            if (option.Denominator == 0)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundDivisionByZero(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            else if (option.Numerator > option.Denominator)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundNotInRange(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            else if (option.Numerator < 0 || option.Denominator < 0)
            {
                Debug.Assert(false);
            }
        }

        if (spectrumDeclaration.DefaultOption is not null && !optionNames.Contains(spectrumDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        if (spectrumDeclaration.Options.Length == 0)
        {
            Debug.Assert(error);
            return (table, null);
        }

        if (error)
        {
            return (table, null);
        }

        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        static int Lcm(int a, int b)
        {
            return a * b / Gcd(a, b);
        }

        int commonDenominator = 1;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            commonDenominator = Lcm(commonDenominator, option.Denominator);
        }

        Debug.Assert(commonDenominator != 0);

        ImmutableDictionary<string, SpectrumInterval>.Builder intervalBuilder = ImmutableDictionary.CreateBuilder<string, SpectrumInterval>();

        (int, int) previousFraction = (0, 1);
        int previousNumerator = 0;
        bool previousInclusive = true;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            // let x be the common denominator
            // then a fraction a/b = (a * (x/b))/x
            // x/b is an integer by definition of lcm
            SpectrumInterval interval = new()
            {
                Inclusive = option.Inclusive,
                UpperDenominator = commonDenominator,
                UpperNumerator = option.Numerator * commonDenominator / option.Denominator,
            };

            if (interval.UpperNumerator <= previousNumerator)
            {
                // this is only okay if the previous one was exclusive and this one is inclusive
                if (previousInclusive || !option.Inclusive)
                {
                    ErrorFound?.Invoke(Errors.SpectrumNotIncreasing(spectrumDeclaration.Name, previousFraction, (option.Numerator, option.Denominator), option.Index));
                    error = true;
                }
            }

            intervalBuilder.Add(option.Name, interval);

            previousFraction = (option.Numerator, option.Denominator);
            previousNumerator = interval.UpperNumerator;
            previousInclusive = interval.Inclusive;
        }

        SpectrumSymbol symbol = new()
        {
            Name = spectrumDeclaration.Name,
            Intervals = intervalBuilder.ToImmutable(),
            OptionNames = spectrumDeclaration.Options.Select(o => o.Name).ToImmutableArray(),
            DefaultOption = spectrumDeclaration.DefaultOption,
            Index = spectrumDeclaration.Index,
        };

        table = table.Declare(symbol);

        return (table, symbol);
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

    private (SymbolTable, ExpressionNode) BindAndTypeExpression(ExpressionNode expression, SymbolTable table)
    {
        switch (expression)
        {
            case IntegerLiteralExpressionNode:
                {
                    Debug.Assert(table.IsDeclared("Int") && table["Int"] is BuiltinTypeSymbol { Type: BuiltinType.Int });

                    TypeSymbol intType = (TypeSymbol)table["Int"];

                    TypedExpressionNode typedExpression = new()
                    {
                        Expression = expression,
                        SourceType = intType,
                        Index = expression.Index,
                    };

                    return (table, typedExpression);
                }
            case StringLiteralExpressionNode:
                {
                    Debug.Assert(table.IsDeclared("String") && table["String"] is BuiltinTypeSymbol { Type: BuiltinType.String });

                    TypeSymbol stringType = (TypeSymbol)table["String"];

                    TypedExpressionNode typedExpression = new()
                    {
                        Expression = expression,
                        SourceType = stringType,
                        Index = expression.Index,
                    };

                    return (table, typedExpression);
                }
            case RecordCreationExpressionNode recordCreationExpression:
                return BindAndTypeRecordCreationExpression(recordCreationExpression, table);
            case IdentifierExpressionNode { Identifier: string identifier, Index: int index }:
                if (!table.IsDeclared(identifier))
                {
                    ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(identifier, index));
                    return (table, expression);
                }
                else // once we get variables or constants, there might actually be symbols to bind to here
                {
                    ErrorFound?.Invoke(Errors.SymbolHasNoValue(identifier, index));
                    return (table, expression);
                }
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, ExpressionNode) BindAndTypeRecordCreationExpression(RecordCreationExpressionNode recordCreation, SymbolTable table)
    {
        if (!table.IsDeclared(recordCreation.RecordName))
        {
            ErrorFound?.Invoke(Errors.RecordDoesNotExist(recordCreation.RecordName, recordCreation.Index));
            return (table, recordCreation);
        }

        if (table[recordCreation.RecordName] is not RecordTypeSymbol recordSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotRecord(recordCreation.RecordName, recordCreation.Index));
            return (table, recordCreation);
        }

        if (recordCreation.Arguments.Length != recordSymbol.Properties.Length)
        {
            ErrorFound?.Invoke(Errors.WrongAmountOfArguments(recordSymbol.Name, recordCreation.Arguments.Length, recordSymbol.Properties.Length, recordCreation.Index));

            TypedExpressionNode incompleteTypedExpression = new()
            {
                Expression = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, incompleteTypedExpression);
        }

        List<ArgumentNode> boundArguments = recordCreation.Arguments.ToList();

        for (int i = 0; i < recordCreation.Arguments.Length; i++)
        {
            if (recordCreation.Arguments[i].PropertyName != null && recordCreation.Arguments[i].PropertyName != recordSymbol.Properties[i].Name)
            {
                ErrorFound?.Invoke(Errors.WrongPropertyInRecordCreation(recordCreation.Arguments[i].PropertyName!, recordCreation.Arguments[i].Index));

                continue;
            }

            TypeSymbol propertyType = recordSymbol.Properties[i].Type;

            (table, ExpressionNode maybeTypedExpression) = BindAndTypeExpression(recordCreation.Arguments[i].Expression, table);

            if (maybeTypedExpression is not TypedExpressionNode typedExpression)
            {
                continue;
            }

            if (!TypesAreCompatible(typedExpression.SourceType, propertyType))
            {
                ErrorFound?.Invoke(Errors.IncompatibleType(typedExpression.SourceType, propertyType, "property", recordCreation.Arguments[i].Index));
                continue;
            }

            typedExpression = typedExpression with
            {
                TargetType = recordSymbol.Properties[i].Type,
            };

            BoundArgumentNode boundArgument = new()
            {
                Expression = typedExpression,
                PropertyName = recordCreation.Arguments[i].PropertyName,
                Property = recordSymbol.Properties[i],
                Index = recordCreation.Index,
            };

            boundArguments[i] = boundArgument;
        }

        if (boundArguments.All(a => a is BoundArgumentNode))
        {
            BoundRecordCreationExpressionNode boundRecordCreation = new()
            {
                CreationExpression = recordCreation,
                BoundArguments = boundArguments.Cast<BoundArgumentNode>().ToImmutableArray(),
                Record = recordSymbol,
                Index = recordCreation.Index,
            };

            TypedExpressionNode typedRecordCreation = new()
            {
                Expression = boundRecordCreation,
                SourceType = recordSymbol,
                Index = boundRecordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
        else
        {
            TypedExpressionNode typedRecordCreation = new()
            {
                Expression = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
    }
}
