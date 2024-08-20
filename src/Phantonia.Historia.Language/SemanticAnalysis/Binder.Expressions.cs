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
            case EnumOptionExpressionNode enumOptionExpression:
                return BindAndTypeEnumOptionExpression(enumOptionExpression, table);
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

        List<ArgumentNode> boundArguments = [.. recordCreation.Arguments];

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
            EnumName = enumOptionExpression.EnumName,
            OptionName = enumOptionExpression.OptionName,
            Index = enumOptionExpression.Index,
            EnumSymbol = enumSymbol,
        };

        TypedExpressionNode typedExpression = new()
        {
            Expression = boundExpression,
            SourceType = enumSymbol,
            Index = boundExpression.Index,
        };

        return (table, typedExpression);
    }
}
