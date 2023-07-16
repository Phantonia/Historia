using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
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

        foreach (SymbolDeclarationNode declaration in story.Symbols)
        {
            if (declaration is not TypeSymbolDeclarationNode)
            {
                continue;
            }

            (table, SymbolDeclarationNode boundDeclaration) = BindPseudoDeclaration(declaration, table);
            boundStory = boundStory with
            {
                Symbols = boundStory.Symbols.Replace(declaration, boundDeclaration),
            };
        }

        return (table, boundStory);
    }

    private (SymbolTable, SymbolDeclarationNode) BindPseudoDeclaration(SymbolDeclarationNode declaration, SymbolTable table)
    {
        switch (declaration)
        {
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindPseudoRecordDeclaration(recordDeclaration, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, SymbolDeclarationNode) BindPseudoRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
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

    private (SymbolTable, StoryNode) BindTree(StoryNode halfboundStory, SymbolTable table)
    {
        List<SymbolDeclarationNode> symbolDeclarations = halfboundStory.Symbols.ToList();

        for (int i = 0; i < symbolDeclarations.Count; i++)
        {
            SymbolDeclarationNode declaration = symbolDeclarations[i];

            if (declaration is TypeSymbolDeclarationNode)
            {
                // already bound these
                continue;
            }

            (table, SymbolDeclarationNode boundDeclaration) = BindDeclaration(declaration, table);
            symbolDeclarations[i] = boundDeclaration;
        }

        StoryNode boundStory = halfboundStory with { Symbols = symbolDeclarations.ToImmutableArray() };
        return (table, boundStory);
    }

    private (SymbolTable, SymbolDeclarationNode) BindDeclaration(SymbolDeclarationNode declaration, SymbolTable table)
    {
        if (declaration is BoundSymbolDeclarationNode { Declaration: var innerDeclaration })
        {
            return BindDeclaration(innerDeclaration, table);
        }

        switch (declaration)
        {
            // this will get more complicated very soon
            case SceneSymbolDeclarationNode sceneDeclaration:
                return BindSceneDeclaration(sceneDeclaration, table);
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindRecordDeclaration(recordDeclaration, table);
            case SettingSymbolDeclarationNode setting:
                return BindSetting(setting, table);
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

    private (SymbolTable, SettingSymbolDeclarationNode) BindSetting(SettingSymbolDeclarationNode setting, SymbolTable table)
    {
        switch (setting)
        {
            case TypeSettingDeclarationNode typeSetting:
                (table, TypeNode boundType) = BindType(typeSetting.Type, table);
                return (table, typeSetting with { Type = boundType });
            case ExpressionSettingDeclarationNode expressionSetting:
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
                        ErrorFound?.Invoke(new Error { ErrorMessage = $"Symbol '{identifierType.Identifier}' does not exist in this scope", Index = identifierType.Index });
                        return (table, type);
                    }

                    Symbol symbol = table[identifierType.Identifier];

                    if (symbol is not TypeSymbol typeSymbol)
                    {
                        ErrorFound?.Invoke(new Error { ErrorMessage = $"Symbol '{identifierType.Identifier}' is not a type but is used like one", Index = identifierType.Index });
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

    private (SymbolTable, BoundSymbolDeclarationNode) BindSceneDeclaration(SceneSymbolDeclarationNode sceneDeclaration, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(sceneDeclaration.Name) && table[sceneDeclaration.Name] is SceneSymbol);

        SceneSymbol sceneSymbol = (SceneSymbol)table[sceneDeclaration.Name];

        (table, StatementBodyNode boundBody) = BindStatementBody(sceneDeclaration.Body, table);

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

    private (SymbolTable, StatementBodyNode) BindStatementBody(StatementBodyNode body, SymbolTable table)
    {
        table = table.OpenScope();

        List<StatementNode> statements = body.Statements.ToList();

        for (int i = 0; i < statements.Count; i++)
        {
            StatementNode statement = statements[i];
            (table, StatementNode boundStatement) = BindStatement(statement, table);
            statements[i] = boundStatement;
        }

        table = table.CloseScope();

        return (table, body with
        {
            Statements = statements.ToImmutableArray(),
        });
    }

    private (SymbolTable, StatementNode) BindStatement(StatementNode statement, SymbolTable table)
    {
        switch (statement)
        {
            case OutputStatementNode outputStatement:
                {
                    (table, ExpressionNode boundExpression) = BindAndTypeExpression(outputStatement.OutputExpression, table);

                    if (boundExpression is TypedExpressionNode { Type: TypeSymbol sourceType })
                    {
                        // we need to get access to the output type here, important
                        // check if that stuff is compatible
                    }

                    OutputStatementNode boundStatement = outputStatement with
                    {
                        OutputExpression = boundExpression,
                    };

                    return (table, boundStatement);
                }
            case SwitchStatementNode switchStatement:
                {
                    (table, ExpressionNode outputExpression) = BindAndTypeExpression(switchStatement.OutputExpression, table);

                    {
                        if (outputExpression is TypedExpressionNode { Type: TypeSymbol sourceType })
                        {
                            // we need to get access to the output type here, important
                            // check if that stuff is compatible
                        }
                    }

                    List<OptionNode> boundOptions = switchStatement.Options.ToList();

                    for (int i = 0; i < boundOptions.Count; i++)
                    {
                        (table, ExpressionNode optionExpression) = BindAndTypeExpression(boundOptions[i].Expression, table);

                        if (optionExpression is TypedExpressionNode { Type: TypeSymbol sourceType })
                        {
                            // we need to get access to the option type here, important
                            // check if that stuff is compatible
                        }

                        (table, StatementBodyNode optionBody) = BindStatementBody(boundOptions[i].Body, table);

                        boundOptions[i] = boundOptions[i] with
                        {
                            Expression = optionExpression,
                            Body = optionBody,
                        };
                    }

                    SwitchStatementNode boundStatement = switchStatement with
                    {
                        OutputExpression = outputExpression,
                        Options = boundOptions.ToImmutableArray(),
                    };

                    return (table, boundStatement);
                }
            default:
                Debug.Assert(false);
                return default;
        }
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
                        Type = intType,
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
                        Type = stringType,
                        Index = expression.Index,
                    };

                    return (table, typedExpression);
                }
            case RecordCreationExpressionNode recordCreationExpression:
                return BindAndTypeRecordCreationExpression(recordCreationExpression, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, ExpressionNode) BindAndTypeRecordCreationExpression(RecordCreationExpressionNode recordCreation, SymbolTable table)
    {
        if (!table.IsDeclared(recordCreation.RecordName))
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = $"Record named '{recordCreation.RecordName}' does not exist", Index = recordCreation.Index });
            return (table, recordCreation);
        }

        if (table[recordCreation.RecordName] is not RecordTypeSymbol recordSymbol)
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = $"Symbol named '{recordCreation.RecordName}' is not a record", Index = recordCreation.Index });
            return (table, recordCreation);
        }

        if (recordCreation.Arguments.Length != recordSymbol.Properties.Length)
        {
            ErrorFound?.Invoke(new Error
            {
                ErrorMessage = $"Record '{recordSymbol.Name}' has {recordSymbol.Properties.Length} properties, but {recordCreation.Arguments.Length} arguments were provided",
                Index = recordCreation.Arguments[0].Index
            });

            TypedExpressionNode incompleteTypedExpression = new()
            {
                Expression = recordCreation,
                Type = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, incompleteTypedExpression);
        }

        List<ArgumentNode> boundArguments = recordCreation.Arguments.ToList();

        for (int i = 0; i < recordCreation.Arguments.Length; i++)
        {
            if (recordCreation.Arguments[i].PropertyName != null && recordCreation.Arguments[i].PropertyName != recordSymbol.Properties[i].Name)
            {
                ErrorFound?.Invoke(new Error
                {
                    ErrorMessage = $"Property '{recordCreation.Arguments[i].PropertyName}' either does not exist or is not in that position",
                    Index = recordCreation.Arguments[i].Index,
                });

                continue;
            }

            TypeSymbol propertyType = recordSymbol.Properties[i].Type;

            (table, ExpressionNode maybeTypedExpression) = BindAndTypeExpression(recordCreation.Arguments[i].Expression, table);

            if (maybeTypedExpression is not TypedExpressionNode typedExpression)
            {
                continue;
            }

            if (!TypesAreCompatible(typedExpression.Type, propertyType))
            {
                ErrorFound?.Invoke(new Error
                {
                    ErrorMessage = $"Expression type '{typedExpression.Type.Name}' is incompatible with property type '{propertyType.Name}'",
                    Index = recordCreation.Arguments[i].Index,
                });
                continue;
            }

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
                Type = recordSymbol,
                Index = boundRecordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
        else
        {
            TypedExpressionNode typedRecordCreation = new()
            {
                Expression = recordCreation,
                Type = recordSymbol,
                Index = recordCreation.Index,
            };

            return (table, typedRecordCreation);
        }
    }
}
