using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
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
                        Original = expression,
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
                        Original = expression,
                        SourceType = stringType,
                        Index = expression.Index,
                    };

                    return (table, typedExpression);
                }
            case RecordCreationExpressionNode recordCreationExpression:
                return BindAndTypeRecordCreationExpression(recordCreationExpression, table);
            case EnumOptionExpressionNode enumOptionExpression:
                return BindAndTypeEnumOptionExpression(enumOptionExpression, table);
            case IsExpressionNode isExpression:
                return BindAndTypeIsExpression(isExpression, table);
            case LogicExpressionNode logicExpression:
                return BindAndTypeLogicExpression(logicExpression, table);
            case NotExpressionNode notExpression:
                return BindAndTypeNotExpression(notExpression, table);
            case IdentifierExpressionNode { Identifier: string identifier, Index: long index }:
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
            ErrorFound?.Invoke(Errors.WrongAmountOfArgumentsInRecordCreation(recordSymbol.Name, recordCreation.Arguments.Length, recordSymbol.Properties.Length, recordCreation.Index));

            TypedExpressionNode incompleteTypedExpression = new()
            {
                Original = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, incompleteTypedExpression);
        }

        (table, List<ArgumentNode> boundArguments) = BindArgumentList(recordCreation, table, recordSymbol.Properties, "property");

        if (boundArguments.All(a => a is BoundArgumentNode))
        {
            BoundRecordCreationExpressionNode boundRecordCreation = new()
            {
                Original = recordCreation,
                BoundArguments = boundArguments.Cast<BoundArgumentNode>().ToImmutableArray(),
                Record = recordSymbol,
                Index = recordCreation.Index,
            };

            TypedExpressionNode typedRecordCreation = new()
            {
                Original = boundRecordCreation,
                SourceType = recordSymbol,
                Index = boundRecordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
        else
        {
            TypedExpressionNode typedRecordCreation = new()
            {
                Original = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
    }

    private (SymbolTable, ExpressionNode) BindAndTypeEnumOptionExpression(EnumOptionExpressionNode enumOptionExpression, SymbolTable table)
    {
        if (!table.IsDeclared(enumOptionExpression.EnumName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(enumOptionExpression.EnumName, enumOptionExpression.Index));
            return (table, enumOptionExpression);
        }

        Symbol symbol = table[enumOptionExpression.EnumName];

        if (symbol is not EnumTypeSymbol enumSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotEnum(enumOptionExpression.EnumName, enumOptionExpression.Index));
            return (table, enumOptionExpression);
        }

        if (!enumSymbol.Options.Contains(enumOptionExpression.OptionName))
        {
            ErrorFound?.Invoke(Errors.OptionDoesNotExistInEnum(enumOptionExpression.EnumName, enumOptionExpression.OptionName, enumOptionExpression.Index));
            return (table, enumOptionExpression);
        }

        BoundEnumOptionExpressionNode boundExpression = new()
        {
            EnumNameToken = enumOptionExpression.EnumNameToken,
            DotToken = enumOptionExpression.DotToken,
            OptionNameToken = enumOptionExpression.OptionNameToken,
            Index = enumOptionExpression.Index,
            EnumSymbol = enumSymbol,
        };

        TypedExpressionNode typedExpression = new()
        {
            Original = boundExpression,
            SourceType = enumSymbol,
            Index = boundExpression.Index,
        };

        return (table, typedExpression);
    }

    private (SymbolTable, ExpressionNode) BindAndTypeIsExpression(IsExpressionNode expression, SymbolTable table)
    {
        if (!table.IsDeclared(expression.OutcomeName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(expression.OutcomeName, expression.Index));
            return (table, expression);
        }

        if (table[expression.OutcomeName] is not OutcomeSymbol outcome)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(expression.OutcomeName, expression.Index));
            return (table, expression);
        }

        if (!outcome.OptionNames.Contains(expression.OptionName))
        {
            ErrorFound?.Invoke(Errors.OptionDoesNotExistInOutcome(outcome.Name, expression.OptionName, expression.Index));
            return (table, expression);
        }

        TypeSymbol booleanType = (TypeSymbol)table["Boolean"];

        TypedExpressionNode typedExpression = new()
        {
            Original = new BoundIsExpressionNode
            {
                Original = expression,
                Outcome = outcome,
                Index = expression.Index,
            },
            SourceType = booleanType,
            Index = expression.Index,
        };

        return (table, typedExpression);
    }

    private (SymbolTable, ExpressionNode) BindAndTypeLogicExpression(LogicExpressionNode logicExpression, SymbolTable table)
    {
        (table, ExpressionNode boundLeftHandSide) = BindAndTypeExpression(logicExpression.LeftExpression, table);
        (table, ExpressionNode boundRightHandSide) = BindAndTypeExpression(logicExpression.RightExpression, table);

        if (boundLeftHandSide is not TypedExpressionNode { SourceType: TypeSymbol leftHandType } || boundRightHandSide is not TypedExpressionNode { SourceType: TypeSymbol rightHandType })
        {
            return (table, logicExpression);
        }

        TypeSymbol booleanType = (TypeSymbol)table["Boolean"];

        bool error = false;

        if (!TypesAreCompatible(leftHandType, booleanType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(leftHandType, booleanType, "operand", logicExpression.LeftExpression.Index));
            error = true;
        }

        if (!TypesAreCompatible(rightHandType, booleanType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(rightHandType, booleanType, "operand", logicExpression.RightExpression.Index));
            error = true;
        }

        if (error)
        {
            return (table, logicExpression);
        }

        TypedExpressionNode typedLeftHandSide = (TypedExpressionNode)boundLeftHandSide with { TargetType = booleanType };
        TypedExpressionNode typedRightHandSide = (TypedExpressionNode)boundRightHandSide with { TargetType = booleanType };

        TypedExpressionNode typedLogicExpression = new()
        {
            Original = logicExpression with
            {
                LeftExpression = typedLeftHandSide,
                RightExpression = typedRightHandSide,
            },
            SourceType = booleanType,
            Index = logicExpression.Index,
        };

        return (table, typedLogicExpression);
    }

    private (SymbolTable, ExpressionNode) BindAndTypeNotExpression(NotExpressionNode notExpression, SymbolTable table)
    {
        (table, ExpressionNode boundInnerExpression) = BindAndTypeExpression(notExpression.InnerExpression, table);

        if (boundInnerExpression is not TypedExpressionNode { SourceType: TypeSymbol innerType })
        {
            return (table, notExpression);
        }

        TypeSymbol booleanType = (TypeSymbol)table["Boolean"];

        if (!TypesAreCompatible(innerType, booleanType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(innerType, booleanType, "operand", notExpression.InnerExpression.Index));
            return (table, notExpression);
        }

        TypedExpressionNode typedInnerExpression = (TypedExpressionNode)boundInnerExpression with { TargetType = booleanType };

        TypedExpressionNode typedNotExpression = new()
        {
            Original = notExpression with
            {
                InnerExpression = typedInnerExpression,
            },
            SourceType = booleanType,
            Index = notExpression.Index,
        };

        return (table, typedNotExpression);
    }
}
