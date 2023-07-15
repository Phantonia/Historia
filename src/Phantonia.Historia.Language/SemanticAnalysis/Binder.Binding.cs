using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Immutable;
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
            case SceneSymbolDeclarationNode:
                return (table, declaration);
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
                (table, ExpressionNode boundExpression) = BindExpression(expressionSetting.Expression, table);
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

    private (SymbolTable, ExpressionNode) BindExpression(ExpressionNode expression, SymbolTable table) => (table, expression); // stub
}
