using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (BindingContext, StatementBodyNode) BindStatementBody(StatementBodyNode body, Settings settings, BindingContext context)
    {
        context = context with
        {
            SymbolTable = context.SymbolTable.OpenScope(),
        };

        List<StatementNode> statements = [.. body.Statements];

        for (int i = 0; i < statements.Count; i++)
        {
            StatementNode statement = statements[i];
            (context, StatementNode boundStatement) = BindStatement(statement, settings, context);
            statements[i] = boundStatement;
        }

        context = context with
        {
            SymbolTable = context.SymbolTable.CloseScope(),
        };

        return (context, body with
        {
            Statements = [.. statements],
        });
    }

    private (BindingContext, StatementNode) BindStatement(StatementNode statement, Settings settings, BindingContext context)
    {
        switch (statement)
        {
            case OutputStatementNode outputStatement:
                return BindOutputStatement(outputStatement, settings, context);
            case LineStatementNode lineStatement:
                return BindLineStatement(lineStatement, settings, context);
            case SwitchStatementNode switchStatement:
                return BindSwitchStatement(switchStatement, settings, context);
            case LoopSwitchStatementNode loopSwitchStatement:
                return BindLoopSwitchStatement(loopSwitchStatement, settings, context);
            case OutcomeDeclarationStatementNode outcomeDeclaration:
                return BindOutcomeDeclarationStatement(outcomeDeclaration, context);
            case SpectrumDeclarationStatementNode spectrumDeclaration:
                return BindSpectrumDeclarationStatement(spectrumDeclaration, context);
            case BranchOnStatementNode branchOnStatement:
                return BindBranchOnStatement(branchOnStatement, settings, context);
            case AssignmentStatementNode assignmentStatement:
                return BindAssignmentStatement(assignmentStatement, context);
            case SpectrumAdjustmentStatementNode adjustmentStatement:
                return BindSpectrumAdjustmentStatement(adjustmentStatement, context);
            case CallStatementNode callStatement:
                return BindCallStatement(callStatement, context);
            case RunStatementNode runStatement:
                return BindRunStatement(runStatement, context);
            case ChooseStatementNode chooseStatement:
                return BindChooseStatement(chooseStatement, settings, context);
            case IfStatementNode ifStatement:
                return BindIfStatement(ifStatement, settings, context);
            case NoOpStatementNode:
                return (context, statement);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (BindingContext, StatementNode) BindOutputStatement(OutputStatementNode outputStatement, Settings settings, BindingContext context)
    {
        bool previousRequiresConstantExpression = context.RequiresConstantExpression;
        context = context with
        {
            RequiresConstantExpression = true,
        };

        (context, ExpressionNode boundExpression) = BindAndTypeExpression(outputStatement.OutputExpression, context);

        if (boundExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
        {
            if (!TypesAreCompatible(sourceType, settings.OutputType))
            {
                ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", boundExpression.Index));

                return (context, outputStatement);
            }

            boundExpression = RecursivelySetTargetType(typedExpression, settings.OutputType);
        }

        OutputStatementNode boundStatement = outputStatement with
        {
            OutputExpression = boundExpression,
        };

        context = context with
        {
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindLineStatement(LineStatementNode lineStatement, Settings settings, BindingContext context)
    {
        RecordTypeSymbol? lineRecord = DetermineApplicableLineRecord(lineStatement, settings, context);

        if (lineRecord is null)
        {
            return (context, lineStatement);
        }

        bool previousRequiresConstantExpression = context.RequiresConstantExpression;
        context = context with
        {
            RequiresConstantExpression = true,
        };

        (context, ExpressionNode boundCharacterExpression) = BindAndTypeCharacterExpression(lineStatement, lineRecord, context);

        List<PropertySymbol> additionalProperties =
            lineRecord.Properties
                      .Skip(1)
                      .Take(lineRecord.Properties.Length - 2)
                      .ToList();

        (context, IReadOnlyList<ArgumentNode>? boundArguments) = BindArgumentList(lineStatement, context, additionalProperties, "property");

        (context, ExpressionNode boundTextExpression) = BindAndTypeExpression(lineStatement.TextExpression, context);

        {
            TypeSymbol targetType = lineRecord.Properties[^1].Type;

            if (boundTextExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, targetType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, targetType, "character", lineStatement.CharacterExpression.Index));
                }
                else
                {
                    boundTextExpression = RecursivelySetTargetType(typedExpression, targetType);
                }
            }
        }

        if (boundArguments.Any(a => a is not BoundArgumentNode))
        {
            return (context, lineStatement);
        }

        IEnumerable<BoundArgumentNode> boundBoundArguments = boundArguments.OfType<BoundArgumentNode>();

        TypedExpressionNode lineCreationExpression = SynthesizeLineCreationExpression(boundCharacterExpression, boundBoundArguments, boundTextExpression, lineRecord, settings);

        BoundLineStatementNode boundLineStatement = new()
        {
            LineStatement = lineStatement with
            {
                CharacterExpression = boundCharacterExpression,
                AdditionalArguments = [.. boundArguments],
                TextExpression = boundTextExpression,
            },
            LineCreationExpression = lineCreationExpression,
            Index = lineStatement.Index,
            PrecedingTokens = [],
        };

        context = context with
        {
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        return (context, boundLineStatement);
    }

    private static TypedExpressionNode SynthesizeLineCreationExpression(
        ExpressionNode boundCharacterExpression,
        IEnumerable<BoundArgumentNode> boundArguments,
        ExpressionNode boundTextExpression,
        RecordTypeSymbol lineRecord,
        Settings settings)
    {
        BoundArgumentNode characterArgument = new()
        {
            Property = lineRecord.Properties[0],
            ParameterNameToken = null,
            EqualsToken = null,
            Expression = boundCharacterExpression,
            CommaToken = Token.Missing(boundCharacterExpression.Index),
            Index = boundCharacterExpression.Index,
            PrecedingTokens = [],
        };

        BoundArgumentNode textArgument = new()
        {
            Property = lineRecord.Properties[^1],
            ParameterNameToken = null,
            EqualsToken = null,
            Expression = boundTextExpression,
            CommaToken = Token.Missing(boundTextExpression.Index),
            Index = boundTextExpression.Index,
            PrecedingTokens = [],
        };

        RecordCreationExpressionNode creationExpression = new()
        {
            RecordNameToken = Token.Missing(characterArgument.Index),
            OpenParenthesisToken = Token.Missing(characterArgument.Index),
            Arguments = [characterArgument, .. boundArguments, textArgument],
            ClosedParenthesisToken = Token.Missing(textArgument.Index),
            Index = boundCharacterExpression.Index,
            PrecedingTokens = [],
        };

        BoundRecordCreationExpressionNode boundCreationExpression = new()
        {
            BoundArguments = [characterArgument, .. boundArguments, textArgument],
            Original = creationExpression,
            Record = lineRecord,
            Index = creationExpression.Index,
            PrecedingTokens = [],
        };

        TypedExpressionNode typedExpression = new()
        {
            Original = boundCreationExpression,
            SourceType = lineRecord,
            TargetType = settings.OutputType,
            Index = boundCreationExpression.Index,
            PrecedingTokens = [],
        };

        return typedExpression;
    }

    private RecordTypeSymbol? DetermineApplicableLineRecord(LineStatementNode lineStatement, Settings settings, BindingContext context)
    {
        int propertyCount = (lineStatement.AdditionalArguments?.Length ?? 0) + 2;

        IEnumerable<RecordTypeSymbol> applicableRecords =
            context.SymbolTable
                   .AllSymbols
                   .OfType<RecordTypeSymbol>()
                   .Where(s => s.IsLineRecord && s.Properties.Length == propertyCount);

        int applicableRecordCount = applicableRecords.Count();

        if (applicableRecordCount == 0)
        {
            ErrorFound?.Invoke(Errors.NoLineRecordWithPropertyCount((lineStatement.AdditionalArguments?.Length ?? 0) + 2, lineStatement.Index));
            return null;
        }

        if (applicableRecordCount > 1)
        {
            ErrorFound?.Invoke(Errors.LineRecordAmbiguous((lineStatement.AdditionalArguments?.Length ?? 0) + 2, applicableRecords.Select(r => r.Name), lineStatement.Index));
            return null;
        }

        RecordTypeSymbol lineRecord = applicableRecords.Single();

        if (!TypesAreCompatible(lineRecord, settings.OutputType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(lineRecord, settings.OutputType, "output", lineStatement.Index));
        }

        return lineRecord;
    }

    private (BindingContext, ExpressionNode) BindAndTypeCharacterExpression(LineStatementNode lineStatement, RecordTypeSymbol lineRecord, BindingContext context)
    {
        ExpressionNode boundCharacterExpression;

        if (lineRecord.Properties[0].Type is EnumTypeSymbol characterEnum
            && lineStatement.CharacterExpression is IdentifierExpressionNode identifierExpression
            && characterEnum.Options.Contains(identifierExpression.Identifier))
        {
            boundCharacterExpression = new TypedExpressionNode
            {
                SourceType = characterEnum,
                TargetType = characterEnum,
                Original = new BoundEnumOptionExpressionNode
                {
                    EnumSymbol = characterEnum,
                    EnumNameToken = Token.Missing(lineStatement.Index),
                    DotToken = Token.Missing(identifierExpression.Index),
                    OptionNameToken = identifierExpression.IdentifierToken,
                    Index = identifierExpression.Index,
                    PrecedingTokens = [],
                },
                Index = identifierExpression.Index,
                PrecedingTokens = [],
            };
        }
        else
        {
            (context, boundCharacterExpression) = BindAndTypeExpression(lineStatement.CharacterExpression, context);

            TypeSymbol targetType = lineRecord.Properties[0].Type;

            if (boundCharacterExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, targetType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, targetType, "character", lineStatement.CharacterExpression.Index));
                }
                else
                {
                    boundCharacterExpression = RecursivelySetTargetType(typedExpression, targetType);
                }
            }
        }

        return (context, boundCharacterExpression);
    }

    private (BindingContext, StatementNode) BindSwitchStatement(SwitchStatementNode switchStatement, Settings settings, BindingContext context)
    {
        bool previousRequiresConstantExpression = context.RequiresConstantExpression;
        context = context with
        {
            RequiresConstantExpression = true,
        };

        (context, ExpressionNode outputExpression) = BindAndTypeExpression(switchStatement.OutputExpression, context);

        {
            if (outputExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OutputType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", outputExpression.Index));
                }
                else
                {
                    outputExpression = RecursivelySetTargetType(typedExpression, settings.OutputType);
                }
            }
        }

        context = context with
        {
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        (context, StatementBodyNode boundBody) = BindStatementBody(switchStatement.Body, settings, context);

        List<OptionNode> boundOptions = [.. switchStatement.Options];

        for (int i = 0; i < boundOptions.Count; i++)
        {
            context = context with
            {
                RequiresConstantExpression = true,
            };

            (context, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, context);

            if (optionExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OptionType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OptionType, "option", optionExpression.Index));
                }
                else
                {
                    optionExpression = RecursivelySetTargetType(typedExpression, settings.OptionType);
                }
            }

            context = context with
            {
                RequiresConstantExpression = previousRequiresConstantExpression,
            };

            (context, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, settings, context);

            boundOptions[i] = boundOptions[i] with
            {
                Expression = optionExpression,
                Body = optionBody,
            };
        }

        SwitchStatementNode boundStatement;

        boundStatement = switchStatement with
        {
            OutputExpression = outputExpression,
            Body = boundBody,
            Options = [.. boundOptions],
        };

        context = context with
        {
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindLoopSwitchStatement(LoopSwitchStatementNode loopSwitchStatement, Settings settings, BindingContext context)
    {
        bool previousIsInLoopSwitch = context.IsInLoopSwitch;
        bool previousRequiresConstantExpression = context.RequiresConstantExpression;
        context = context with
        {
            IsInLoopSwitch = true,
            RequiresConstantExpression = true,
        };

        (context, ExpressionNode outputExpression) = BindAndTypeExpression(loopSwitchStatement.OutputExpression, context);

        {
            if (outputExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OutputType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OutputType, "output", outputExpression.Index));
                }
                else
                {
                    outputExpression = RecursivelySetTargetType(typedExpression, settings.OutputType);
                }
            }
        }

        List<LoopSwitchOptionNode> boundOptions = [.. loopSwitchStatement.Options];

        for (int i = 0; i < boundOptions.Count; i++)
        {
            context = context with
            {
                RequiresConstantExpression = true,
            };

            (context, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, context);

            if (optionExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OptionType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OptionType, "option", optionExpression.Index));
                }
                else
                {
                    optionExpression = RecursivelySetTargetType(typedExpression, settings.OptionType);
                }
            }

            context = context with
            {
                RequiresConstantExpression = previousRequiresConstantExpression,
            };

            (context, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, settings, context);

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
            Options = [.. boundOptions],
        };

        context = context with
        {
            IsInLoopSwitch = previousIsInLoopSwitch,
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindOutcomeDeclarationStatement(OutcomeDeclarationStatementNode outcomeDeclaration, BindingContext context)
    {
        (context, OutcomeSymbol? symbol) = BindOutcomeDeclaration(outcomeDeclaration, context);

        if (symbol is null)
        {
            return (context, outcomeDeclaration);
        }

        BoundOutcomeDeclarationStatementNode boundStatement = new()
        {
            Outcome = symbol,
            OutcomeKeywordToken = outcomeDeclaration.OutcomeKeywordToken,
            NameToken = outcomeDeclaration.NameToken,
            OpenParenthesisToken = outcomeDeclaration.OpenParenthesisToken,
            OptionNameTokens = outcomeDeclaration.OptionNameTokens,
            CommaTokens = outcomeDeclaration.CommaTokens,
            ClosedParenthesisToken = outcomeDeclaration.ClosedParenthesisToken,
            DefaultKeywordToken = outcomeDeclaration.DefaultKeywordToken,
            DefaultOptionToken = outcomeDeclaration.DefaultOptionToken,
            SemicolonToken = outcomeDeclaration.SemicolonToken,
            Index = outcomeDeclaration.Index,
            PrecedingTokens = [],
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindSpectrumDeclarationStatement(SpectrumDeclarationStatementNode spectrumDeclaration, BindingContext context)
    {
        (context, SpectrumSymbol? symbol) = BindSpectrumDeclaration(spectrumDeclaration, context);

        if (symbol is null)
        {
            return (context, spectrumDeclaration);
        }

        BoundSpectrumDeclarationStatementNode boundStatement = new()
        {
            Spectrum = symbol,
            SpectrumKeywordToken = spectrumDeclaration.SpectrumKeywordToken,
            NameToken = spectrumDeclaration.NameToken,
            OpenParenthesisToken = spectrumDeclaration.OpenParenthesisToken,
            Options = spectrumDeclaration.Options,
            ClosedParenthesisToken = spectrumDeclaration.ClosedParenthesisToken,
            DefaultKeywordToken = spectrumDeclaration.DefaultKeywordToken,
            DefaultOptionToken = spectrumDeclaration.DefaultOptionToken,
            SemicolonToken = spectrumDeclaration.SemicolonToken,
            Index = spectrumDeclaration.Index,
            PrecedingTokens = [],
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindAssignmentStatement(AssignmentStatementNode assignmentStatement, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(assignmentStatement.VariableName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(assignmentStatement.VariableName, assignmentStatement.Index));
            return (context, assignmentStatement);
        }

        Symbol symbol = context.SymbolTable[assignmentStatement.VariableName];

        switch (symbol)
        {
            case OutcomeSymbol outcomeSymbol and not SpectrumSymbol: // spectrums are outcomes but cannot be assigned to
                {
                    if (assignmentStatement.AssignedExpression is not IdentifierExpressionNode { Identifier: string option })
                    {
                        ErrorFound?.Invoke(Errors.OutcomeAssignedNonIdentifier(assignmentStatement.VariableName, assignmentStatement.AssignedExpression.Index));
                        return (context, assignmentStatement);
                    }

                    if (!outcomeSymbol.OptionNames.Contains(option))
                    {
                        ErrorFound?.Invoke(Errors.OptionDoesNotExistInOutcome(assignmentStatement.VariableName, option, assignmentStatement.AssignedExpression.Index));
                        return (context, assignmentStatement);
                    }

                    BoundOutcomeAssignmentStatementNode boundAssignment = new()
                    {
                        Outcome = outcomeSymbol,
                        VariableNameToken = assignmentStatement.VariableNameToken,
                        EqualsSignToken = assignmentStatement.EqualsSignToken,
                        AssignedExpression = assignmentStatement.AssignedExpression,
                        SemicolonToken = assignmentStatement.SemicolonToken,
                        Index = assignmentStatement.Index,
                        PrecedingTokens = [],
                    };

                    return (context, boundAssignment);
                }
            default:
                ErrorFound?.Invoke(Errors.SymbolCannotBeAssignedTo(symbol.Name, assignmentStatement.Index));
                return (context, assignmentStatement);
        }
    }

    private (BindingContext, StatementNode) BindSpectrumAdjustmentStatement(SpectrumAdjustmentStatementNode adjustmentStatement, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(adjustmentStatement.SpectrumName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(adjustmentStatement.SpectrumName, adjustmentStatement.Index));
            return (context, adjustmentStatement);
        }

        Symbol symbol = context.SymbolTable[adjustmentStatement.SpectrumName];

        if (symbol is not SpectrumSymbol spectrumSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotSpectrum(adjustmentStatement.SpectrumName, adjustmentStatement.Index));
            return (context, adjustmentStatement);
        }

        (context, ExpressionNode amount) = BindAndTypeExpression(adjustmentStatement.AdjustmentAmount, context);

        if (amount is not TypedExpressionNode typedAmount)
        {
            return (context, adjustmentStatement);
        }

        if (!TypesAreCompatible(typedAmount.SourceType, (TypeSymbol)context.SymbolTable["Int"]))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(typedAmount.SourceType, (TypeSymbol)context.SymbolTable["Int"], "strengthen/weaken amount", typedAmount.Index));
        }

        typedAmount = RecursivelySetTargetType(typedAmount, (TypeSymbol)context.SymbolTable["Int"]);

        BoundSpectrumAdjustmentStatementNode boundStatement = new()
        {
            Spectrum = spectrumSymbol,
            StrengthenOrWeakenKeywordToken = adjustmentStatement.StrengthenOrWeakenKeywordToken,
            SpectrumNameToken = adjustmentStatement.SpectrumNameToken,
            ByKeywordToken = adjustmentStatement.ByKeywordToken,
            AdjustmentAmount = typedAmount,
            SemicolonToken = adjustmentStatement.SemicolonToken,
            Index = adjustmentStatement.Index,
            PrecedingTokens = [],
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindBranchOnStatement(BranchOnStatementNode branchOnStatement, Settings settings, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(branchOnStatement.OutcomeName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(branchOnStatement.OutcomeName, branchOnStatement.Index));
            return (context, branchOnStatement);
        }

        if (context.SymbolTable[branchOnStatement.OutcomeName] is not OutcomeSymbol outcomeSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(branchOnStatement.OutcomeName, branchOnStatement.Index));
            return (context, branchOnStatement);
        }

        HashSet<string> uniqueOptionNames = [];
        List<BranchOnOptionNode> boundOptions = [];

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

            (context, StatementBodyNode boundBody) = BindStatementBody(option.Body, settings, context);

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
            BranchOnKeywordToken = branchOnStatement.BranchOnKeywordToken,
            OutcomeNameToken = branchOnStatement.OutcomeNameToken,
            OpenBraceToken = branchOnStatement.OpenBraceToken,
            Options = [.. boundOptions],
            ClosedBraceToken = branchOnStatement.ClosedBraceToken,
            Index = branchOnStatement.Index,
            PrecedingTokens = [],
        };

        return (context, boundStatement);
    }

    private (BindingContext, StatementNode) BindCallStatement(CallStatementNode callStatement, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(callStatement.SubroutineName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(callStatement.SubroutineName, callStatement.Index));
            return (context, callStatement);
        }

        if (context.SymbolTable[callStatement.SubroutineName] is not SubroutineSymbol subroutine)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(callStatement.SubroutineName, callStatement.Index));
            return (context, callStatement);
        }

        if (context.IsInScene && subroutine.IsChapter)
        {
            ErrorFound?.Invoke(Errors.SceneCallsChapter(subroutine.Name, callStatement.Index));
        }

        if (context.IsInLoopSwitch && subroutine.IsChapter)
        {
            ErrorFound?.Invoke(Errors.ChapterCalledInLoopSwitch(subroutine.Name, callStatement.Index));
        }

        BoundCallStatementNode boundCallStatement = new()
        {
            Subroutine = subroutine,
            CallKeywordToken = callStatement.CallKeywordToken,
            SubroutineNameToken = callStatement.SubroutineNameToken,
            SemicolonToken = callStatement.SemicolonToken,
            Index = callStatement.Index,
            PrecedingTokens = [],
        };

        return (context, boundCallStatement);
    }

    private (BindingContext, StatementNode) BindRunStatement(RunStatementNode runStatement, BindingContext context)
    {
        (context, ReferenceSymbol? referenceSymbol, InterfaceMethodSymbol? methodSymbol, IEnumerable<BoundArgumentNode>? boundArguments) = BindMethodCallStatement(runStatement, context);

        if (referenceSymbol is null || methodSymbol is null || boundArguments is null)
        {
            return (context, runStatement);
        }

        BoundRunStatementNode boundRunStatement = new()
        {
            Reference = referenceSymbol,
            Method = methodSymbol,
            Original = runStatement,
            Arguments = [.. boundArguments],
            Index = runStatement.Index,
            PrecedingTokens = [],
        };

        return (context, boundRunStatement);
    }

    private (BindingContext, StatementNode) BindChooseStatement(ChooseStatementNode chooseStatement, Settings settings, BindingContext context)
    {
        (context, ReferenceSymbol? referenceSymbol, InterfaceMethodSymbol? methodSymbol, IEnumerable<BoundArgumentNode>? boundArguments) = BindMethodCallStatement(chooseStatement, context);

        if (referenceSymbol is null || methodSymbol is null || boundArguments is null)
        {
            return (context, chooseStatement);
        }

        List<OptionNode> boundOptions = [.. chooseStatement.Options];

        for (int i = 0; i < boundOptions.Count; i++)
        {
            (context, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, context);

            if (optionExpression is TypedExpressionNode { SourceType: TypeSymbol sourceType } typedExpression)
            {
                if (!TypesAreCompatible(sourceType, settings.OptionType))
                {
                    ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, settings.OptionType, "option", optionExpression.Index));
                }
                else
                {
                    optionExpression = RecursivelySetTargetType(typedExpression, settings.OptionType);
                }
            }

            (context, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, settings, context);

            boundOptions[i] = boundOptions[i] with
            {
                Expression = optionExpression,
                Body = optionBody,
            };
        }

        BoundChooseStatementNode boundChooseStatement = new()
        {
            Reference = referenceSymbol,
            Method = methodSymbol,
            Arguments = [.. boundArguments],
            Options = [.. boundOptions],
            Original = chooseStatement,
            Index = chooseStatement.Index,
            PrecedingTokens = [],
        };

        return (context, boundChooseStatement);
    }

    private (BindingContext, StatementNode) BindIfStatement(IfStatementNode ifStatement, Settings settings, BindingContext context)
    {
        (context, ExpressionNode boundCondition) = BindAndTypeExpression(ifStatement.Condition, context);

        if (boundCondition is TypedExpressionNode { SourceType: TypeSymbol sourceType })
        {
            TypeSymbol booleanType = (TypeSymbol)context.SymbolTable["Boolean"];

            if (!TypesAreCompatible(sourceType, booleanType))
            {
                ErrorFound?.Invoke(Errors.IncompatibleType(sourceType, booleanType, "condition", boundCondition.Index));
            }
            ;

            boundCondition = RecursivelySetTargetType((TypedExpressionNode)boundCondition, booleanType);
        }

        (context, StatementBodyNode? boundThenBlock) = BindStatementBody(ifStatement.ThenBlock, settings, context);

        StatementBodyNode? boundElseBlock = null;

        if (ifStatement.ElseBlock is not null)
        {
            (context, boundElseBlock) = BindStatementBody(ifStatement.ElseBlock, settings, context);
        }

        IfStatementNode boundStatement = ifStatement with
        {
            Condition = boundCondition,
            ThenBlock = boundThenBlock,
            ElseBlock = boundElseBlock,
        };

        return (context, boundStatement);
    }

    private (BindingContext context, ReferenceSymbol? referenceSymbol, InterfaceMethodSymbol? methodSymbol, IEnumerable<BoundArgumentNode>? boundArguments)
        BindMethodCallStatement(MethodCallStatementNode methodCallStatement, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(methodCallStatement.ReferenceName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(methodCallStatement.ReferenceName, methodCallStatement.Index));
            return (context, null, null, null);
        }

        if (context.SymbolTable[methodCallStatement.ReferenceName] is not ReferenceSymbol referenceSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotReference(methodCallStatement.ReferenceName, methodCallStatement.Index));
            return (context, null, null, null);
        }

        InterfaceSymbol interfaceSymbol = referenceSymbol.Interface;

        InterfaceMethodSymbol? methodSymbol = interfaceSymbol.Methods.FirstOrDefault(m => m.Name == methodCallStatement.MethodName);

        if (methodSymbol is null)
        {
            ErrorFound?.Invoke(Errors.MethodDoesNotExistInInterface(referenceSymbol.Name, interfaceSymbol.Name, methodCallStatement.MethodName, methodCallStatement.Index));
            return (context, null, null, null);
        }

        InterfaceMethodKind expectedKind = methodCallStatement switch
        {
            RunStatementNode => InterfaceMethodKind.Action,
            ChooseStatementNode => InterfaceMethodKind.Choice,
            _ => default,
        };

        if (methodSymbol.Kind != expectedKind)
        {
            switch (expectedKind)
            {
                case InterfaceMethodKind.Action:
                    ErrorFound?.Invoke(Errors.CannotRunChoiceMethod(interfaceSymbol.Name, methodSymbol.Name, methodCallStatement.Index));
                    break;
                case InterfaceMethodKind.Choice:
                    ErrorFound?.Invoke(Errors.CannotChooseFromActionMethod(interfaceSymbol.Name, methodSymbol.Name, methodCallStatement.Index));
                    break;
            }

            return (context, null, null, null);
        }

        if (methodCallStatement.Arguments.Length != methodSymbol.Parameters.Length)
        {
            ErrorFound?.Invoke(Errors.WrongAmountOfArgumentsInMethodCall(interfaceSymbol.Name, methodSymbol.Name, methodCallStatement.Arguments.Length, methodSymbol.Parameters.Length, methodCallStatement.Index));
            return (context, null, null, null);
        }

        (context, IReadOnlyList<ArgumentNode> boundArguments) = BindArgumentList(methodCallStatement, context, methodSymbol.Parameters, "parameter");

        if (!boundArguments.All(a => a is BoundArgumentNode))
        {
            return (context, null, null, null);
        }

        return (context, referenceSymbol, methodSymbol, boundArguments.Cast<BoundArgumentNode>());
    }
}
