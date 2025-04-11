using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (BindingContext, ExpressionNode) BindAndTypeExpression(ExpressionNode expression, BindingContext context)
    {
        bool previousRequiresConstantExpression = context.RequiresConstantExpression;

        if (context.RequiresConstantExpression && !expression.IsConstant)
        {
            ErrorFound?.Invoke(Errors.ExpectedConstantExpression(expression.Index));

            // we don't require nested expressions to be constant anymore
            context = context with
            {
                RequiresConstantExpression = false,
            };
        }

        ExpressionNode resultExpression;

        switch (expression)
        {
            case ParenthesizedExpressionNode parenthesizedExpression:
                {
                    (context, ExpressionNode innerExpression) = BindAndTypeExpression(parenthesizedExpression.InnerExpression, context);

                    parenthesizedExpression = parenthesizedExpression with
                    {
                        InnerExpression = innerExpression,
                    };

                    if (innerExpression is not TypedExpressionNode typedInnerExpression)
                    {
                        return (context, parenthesizedExpression);
                    }

                    TypedExpressionNode typedExpression = new()
                    {
                        Original = parenthesizedExpression,
                        SourceType = typedInnerExpression.SourceType,
                        Index = parenthesizedExpression.Index,
                        PrecedingTokens = [],
                    };

                    resultExpression = typedExpression;
                    break;
                }
            case IntegerLiteralExpressionNode:
                (context, resultExpression) = BindToType(expression, "Int", context);
                break;
            case StringLiteralExpressionNode:
                (context, resultExpression) = BindToType(expression, "String", context);
                break;
            case BooleanLiteralExpressionNode:
                (context, resultExpression) = BindToType(expression, "Boolean", context);
                break;
            case RecordCreationExpressionNode recordCreationExpression:
                (context, resultExpression) = BindAndTypeRecordCreationExpression(recordCreationExpression, context);
                break;
            case EnumOptionExpressionNode enumOptionExpression:
                (context, resultExpression) = BindAndTypeEnumOptionExpression(enumOptionExpression, context);
                break;
            case IsExpressionNode isExpression:
                (context, resultExpression) = BindAndTypeIsExpression(isExpression, context);
                break;
            case LogicExpressionNode logicExpression:
                (context, resultExpression) = BindAndTypeLogicExpression(logicExpression, context);
                break;
            case NotExpressionNode notExpression:
                (context, resultExpression) = BindAndTypeNotExpression(notExpression, context);
                break;
            case IntegerNegationExpressionNode negationExpression:
                (context, resultExpression) = BindAndTypeNegationExpression(negationExpression, context);
                break;
            case IdentifierExpressionNode { Identifier: string identifier, Index: long index }:
                if (!context.SymbolTable.IsDeclared(identifier))
                {
                    ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(identifier, index));
                }
                else // once we get variables or constants, there might actually be symbols to bind to here
                {
                    ErrorFound?.Invoke(Errors.SymbolHasNoValue(identifier, index));
                }

                resultExpression = expression;
                break;
            case MissingExpressionNode:
                resultExpression = expression;
                break;
            default:
                Debug.Assert(false);
                return default;
        }

        context = context with
        {
            RequiresConstantExpression = previousRequiresConstantExpression,
        };

        return (context, resultExpression);
    }

    private (BindingContext, ExpressionNode) BindToType(ExpressionNode expression, string typeName, BindingContext context)
    {
        Debug.Assert(context.SymbolTable.IsDeclared(typeName) && context.SymbolTable[typeName] is TypeSymbol);

        TypeSymbol stringType = (TypeSymbol)context.SymbolTable[typeName];

        TypedExpressionNode typedExpression = new()
        {
            Original = expression,
            SourceType = stringType,
            Index = expression.Index,
            PrecedingTokens = [],
        };

        return (context, typedExpression);
    }

    private (BindingContext, ExpressionNode) BindAndTypeRecordCreationExpression(RecordCreationExpressionNode recordCreation, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(recordCreation.RecordName))
        {
            ErrorFound?.Invoke(Errors.RecordDoesNotExist(recordCreation.RecordName, recordCreation.Index));
            return (context, recordCreation);
        }

        if (context.SymbolTable[recordCreation.RecordName] is not RecordTypeSymbol recordSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotRecord(recordCreation.RecordName, recordCreation.Index));
            return (context, recordCreation);
        }

        if (recordCreation.Arguments.Length != recordSymbol.Properties.Length)
        {
            ErrorFound?.Invoke(Errors.WrongAmountOfArgumentsInRecordCreation(recordSymbol.Name, recordCreation.Arguments.Length, recordSymbol.Properties.Length, recordCreation.Index));

            TypedExpressionNode incompleteTypedExpression = new()
            {
                Original = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
                PrecedingTokens = [],
            };

            return (context, incompleteTypedExpression);
        }

        (context, IReadOnlyList<ArgumentNode> boundArguments) = BindArgumentList(recordCreation, context, recordSymbol.Properties, "property");

        if (boundArguments.All(a => a is BoundArgumentNode))
        {
            BoundRecordCreationExpressionNode boundRecordCreation = new()
            {
                Original = recordCreation,
                BoundArguments = boundArguments.Cast<BoundArgumentNode>().ToImmutableArray(),
                Record = recordSymbol,
                Index = recordCreation.Index,
                PrecedingTokens = [],
            };

            TypedExpressionNode typedRecordCreation = new()
            {
                Original = boundRecordCreation,
                SourceType = recordSymbol,
                Index = boundRecordCreation.Index,
                PrecedingTokens = [],
            };

            return (context, typedRecordCreation);
        }
        else
        {
            TypedExpressionNode typedRecordCreation = new()
            {
                Original = recordCreation,
                SourceType = recordSymbol,
                Index = recordCreation.Index,
                PrecedingTokens = [],
            };

            return (context, typedRecordCreation);
        }
    }

    private (BindingContext, ExpressionNode) BindAndTypeEnumOptionExpression(EnumOptionExpressionNode enumOptionExpression, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(enumOptionExpression.EnumName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(enumOptionExpression.EnumName, enumOptionExpression.Index));
            return (context, enumOptionExpression);
        }

        Symbol symbol = context.SymbolTable[enumOptionExpression.EnumName];

        if (symbol is not EnumTypeSymbol enumSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotEnum(enumOptionExpression.EnumName, enumOptionExpression.Index));
            return (context, enumOptionExpression);
        }

        if (!enumSymbol.Options.Contains(enumOptionExpression.OptionName))
        {
            ErrorFound?.Invoke(Errors.OptionDoesNotExistInEnum(enumOptionExpression.EnumName, enumOptionExpression.OptionName, enumOptionExpression.Index));
            return (context, enumOptionExpression);
        }

        BoundEnumOptionExpressionNode boundExpression = new()
        {
            EnumNameToken = enumOptionExpression.EnumNameToken,
            DotToken = enumOptionExpression.DotToken,
            OptionNameToken = enumOptionExpression.OptionNameToken,
            Index = enumOptionExpression.Index,
            EnumSymbol = enumSymbol,
            PrecedingTokens = [],
        };

        TypedExpressionNode typedExpression = new()
        {
            Original = boundExpression,
            SourceType = enumSymbol,
            Index = boundExpression.Index,
            PrecedingTokens = [],
        };

        return (context, typedExpression);
    }

    private (BindingContext, ExpressionNode) BindAndTypeIsExpression(IsExpressionNode expression, BindingContext context)
    {
        if (!context.SymbolTable.IsDeclared(expression.OutcomeName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(expression.OutcomeName, expression.Index));
            return (context, expression);
        }

        if (context.SymbolTable[expression.OutcomeName] is not OutcomeSymbol outcome)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotOutcome(expression.OutcomeName, expression.Index));
            return (context, expression);
        }

        if (!outcome.OptionNames.Contains(expression.OptionName))
        {
            ErrorFound?.Invoke(Errors.OptionDoesNotExistInOutcome(outcome.Name, expression.OptionName, expression.Index));
            return (context, expression);
        }

        TypeSymbol booleanType = (TypeSymbol)context.SymbolTable["Boolean"];

        TypedExpressionNode typedExpression = new()
        {
            Original = new BoundIsExpressionNode
            {
                Original = expression,
                Outcome = outcome,
                Index = expression.Index,
                PrecedingTokens = [],
            },
            SourceType = booleanType,
            Index = expression.Index,
            PrecedingTokens = [],
        };

        return (context, typedExpression);
    }

    private (BindingContext, ExpressionNode) BindAndTypeLogicExpression(LogicExpressionNode logicExpression, BindingContext context)
    {
        (context, ExpressionNode boundLeftHandSide) = BindAndTypeExpression(logicExpression.LeftExpression, context);
        (context, ExpressionNode boundRightHandSide) = BindAndTypeExpression(logicExpression.RightExpression, context);

        if (boundLeftHandSide is not TypedExpressionNode { SourceType: TypeSymbol leftHandType } || boundRightHandSide is not TypedExpressionNode { SourceType: TypeSymbol rightHandType })
        {
            return (context, logicExpression);
        }

        TypeSymbol booleanType = (TypeSymbol)context.SymbolTable["Boolean"];

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
            return (context, logicExpression);
        }

        TypedExpressionNode typedLeftHandSide = RecursivelySetTargetType((TypedExpressionNode)boundLeftHandSide, booleanType);
        TypedExpressionNode typedRightHandSide = RecursivelySetTargetType((TypedExpressionNode)boundRightHandSide, booleanType);

        TypedExpressionNode typedLogicExpression = new()
        {
            Original = logicExpression with
            {
                LeftExpression = typedLeftHandSide,
                RightExpression = typedRightHandSide,
                PrecedingTokens = [],
            },
            SourceType = booleanType,
            Index = logicExpression.Index,
            PrecedingTokens = [],
        };

        return (context, typedLogicExpression);
    }

    private (BindingContext, ExpressionNode) BindAndTypeNotExpression(NotExpressionNode notExpression, BindingContext context)
    {
        (context, ExpressionNode boundInnerExpression) = BindAndTypeExpression(notExpression.InnerExpression, context);

        if (boundInnerExpression is not TypedExpressionNode { SourceType: TypeSymbol innerType })
        {
            return (context, notExpression);
        }

        TypeSymbol booleanType = (TypeSymbol)context.SymbolTable["Boolean"];

        if (!TypesAreCompatible(innerType, booleanType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(innerType, booleanType, "operand", notExpression.InnerExpression.Index));
            return (context, notExpression);
        }

        TypedExpressionNode typedInnerExpression = RecursivelySetTargetType((TypedExpressionNode)boundInnerExpression, booleanType);

        TypedExpressionNode typedNotExpression = new()
        {
            Original = notExpression with
            {
                InnerExpression = typedInnerExpression,
            },
            SourceType = booleanType,
            Index = notExpression.Index,
            PrecedingTokens = [],
        };

        return (context, typedNotExpression);
    }

    private (BindingContext, ExpressionNode) BindAndTypeNegationExpression(IntegerNegationExpressionNode negationExpression, BindingContext context)
    {
        (context, ExpressionNode boundInnerExpression) = BindAndTypeExpression(negationExpression.InnerExpression, context);

        if (boundInnerExpression is not TypedExpressionNode { SourceType: TypeSymbol innerType })
        {
            return (context, negationExpression);
        }

        TypeSymbol intType = (TypeSymbol)context.SymbolTable["Int"];

        if (!TypesAreCompatible(innerType, intType))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(innerType, intType, "operand", negationExpression.InnerExpression.Index));
            return (context, negationExpression);
        }

        TypedExpressionNode typedInnerExpression = RecursivelySetTargetType((TypedExpressionNode)boundInnerExpression, intType);

        TypedExpressionNode typedNegationExpression = new()
        {
            Original = negationExpression with
            {
                InnerExpression = typedInnerExpression,
            },
            SourceType = intType,
            Index = negationExpression.Index,
            PrecedingTokens = [],
        };

        return (context, typedNegationExpression);
    }
}
