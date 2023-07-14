using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

// boobies begone
public sealed class Binder
{
    public Binder(StoryNode story)
    {
        this.story = story;
    }

    private readonly StoryNode story;

    public event Action<Error>? ErrorFound;

    // the resulting symbol table is supposed to include all top-level symbols, not in any deeper scope
    public BindingResult Bind()
    {
        SymbolTable table = GetBuiltinSymbolTable();
        table = CollectTopLevelSymbols(table);

        if (!table.IsDeclared("main") || table["main"] is not SceneSymbol)
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = "A story needs a main scene", Index = 0 });
        }

        (table, StoryNode boundStory) = BindTree(table);

        return new BindingResult(boundStory, table);
    }

    private static SymbolTable GetBuiltinSymbolTable()
    {
        SymbolTable symbolTable = new();
        symbolTable = symbolTable.OpenScope()
                                 .Declare(new BuiltinTypeSymbol { Name = "Int", Type = BuiltinType.Int })
                                 .Declare(new BuiltinTypeSymbol { Name = "String", Type = BuiltinType.String });
        return symbolTable;
    }

    private SymbolTable CollectTopLevelSymbols(SymbolTable table)
    {
        table = table.OpenScope();

        foreach (SymbolDeclarationNode declaration in story.Symbols)
        {
            Symbol? newSymbol = CreateSymbolFromDeclaration(declaration);

            if (newSymbol is null)
            {
                // this is not a symbol we need to but into the symbol table
                continue;
            }

            if (table.IsDeclared(newSymbol.Name))
            {
                ErrorFound?.Invoke(new Error { ErrorMessage = $"Duplicated symbol name '{newSymbol.Name}'", Index = declaration.Index });
            }
            else
            {
                table = table.Declare(newSymbol);
            }
        }

        return table;
    }

    private static Symbol? CreateSymbolFromDeclaration(SymbolDeclarationNode declaration)
    {
        switch (declaration)
        {
            case SceneSymbolDeclarationNode { Name: string name }:
                return new SceneSymbol { Name = name };
            case RecordSymbolDeclarationNode recordDeclaration:
                return CreateRecordSymbolFromDeclaration(recordDeclaration);
            case SettingSymbolDeclarationNode:
                return null;
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private static RecordTypeSymbol CreateRecordSymbolFromDeclaration(RecordSymbolDeclarationNode recordDeclaration)
    {
        ImmutableArray<PropertySymbol>.Builder properties = ImmutableArray.CreateBuilder<PropertySymbol>();

        foreach (PropertyDeclarationNode propertyDeclaration in recordDeclaration.Properties)
        {
            properties.Add(new PropertySymbol { Name = propertyDeclaration.Name, Type = propertyDeclaration.Type });
        }

        return new RecordTypeSymbol
        {
            Name = recordDeclaration.Name,
            Properties = properties.ToImmutable(),
        };
    }

    private (SymbolTable, StoryNode) BindTree(SymbolTable table)
    {
        StoryNode boundStory = story;

        foreach (SymbolDeclarationNode declaration in story.Symbols)
        {
            (table, SymbolDeclarationNode boundDeclaration) = BindDeclaration(declaration, table);
            boundStory = boundStory with
            {
                Symbols = boundStory.Symbols.Replace(declaration, boundDeclaration),
            };
        }

        return (table, boundStory);
    }

    private (SymbolTable, SymbolDeclarationNode) BindDeclaration(SymbolDeclarationNode declaration, SymbolTable table)
    {
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

    private (SymbolTable, SymbolDeclarationNode) BindRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
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
