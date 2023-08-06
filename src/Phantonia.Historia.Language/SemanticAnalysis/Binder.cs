using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SemanticAnalysis;

// boobies begone
// sanity also begone
public sealed partial class Binder
{
    // binding procedure
    /* 1. collect top level symbols
     * 2. bind everything except for scene bodies into pseudo symbols
     * 3. build dependency graph + check it is not cyclic
     * 4. fix pseudo symbols into true symbols
     * 5. bind whole tree
     * for whole explanation see notion
     */

    public Binder(StoryNode story)
    {
        this.story = story;
    }

    private readonly StoryNode story;

    public event Action<Error>? ErrorFound;

    // the resulting symbol table is supposed to include all top-level symbols, not in any deeper scope
    public BindingResult Bind()
    {
        // 1. collect top level symbols
        SymbolTable table = GetBuiltinSymbolTable();
        table = CollectTopLevelSymbols(table);

        if (!table.IsDeclared("main") || table["main"] is not SceneSymbol)
        {
            ErrorFound?.Invoke(Errors.NoMainScene());
        }

        // 2. bind everything except for scene bodies into pseudo symbols
        (table, StoryNode halfboundStory) = BindPseudoSymbolDeclarations(table);

        // 3. build dependency graph + check it is not cyclic
        DependencyGraph? dependencyGraph = BuildTypeDependencyGraph(halfboundStory, table);
        if (dependencyGraph is null)
        {
            // return early - cyclic dependency
            return BindingResult.Invalid;
        }

        // 4. fix pseudo symbols into true symbols
        table = FixPseudoSymbols(dependencyGraph, table);

        // 5. bind settings
        (table, Settings settings, halfboundStory) = BindSettingDirectives(halfboundStory, table);

        // 5. bind whole tree
        (table, StoryNode boundStory) = BindTree(halfboundStory, settings, table);

        return new BindingResult(boundStory, settings, table);
    }

    private static SymbolTable GetBuiltinSymbolTable()
    {
        SymbolTable symbolTable = new();
        symbolTable = symbolTable.OpenScope()
                                 .Declare(new BuiltinTypeSymbol { Name = "Int", Type = BuiltinType.Int, Index = -1 })
                                 .Declare(new BuiltinTypeSymbol { Name = "String", Type = BuiltinType.String, Index = -2 });
        return symbolTable;
    }

    private SymbolTable CollectTopLevelSymbols(SymbolTable table)
    {
        table = table.OpenScope();

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
            Symbol? newSymbol = CreateSymbolFromDeclaration(declaration);

            if (newSymbol is null)
            {
                // this is not a symbol we need to put into the symbol table
                continue;
            }

            if (table.IsDeclared(newSymbol.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedSymbolName(newSymbol.Name, declaration.Index));
            }
            else
            {
                table = table.Declare(newSymbol);
            }
        }

        return table;
    }

    private static Symbol? CreateSymbolFromDeclaration(TopLevelNode declaration)
    {
        switch (declaration)
        {
            case SceneSymbolDeclarationNode { Name: string name, Index: int index }:
                return new SceneSymbol { Name = name, Index = index };
            case RecordSymbolDeclarationNode recordDeclaration:
                return CreateRecordSymbolFromDeclaration(recordDeclaration);
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                return CreateUnionSymbolFromDeclaration(unionDeclaration);
            case SettingDirectiveNode:
                return null;
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private static PseudoRecordTypeSymbol CreateRecordSymbolFromDeclaration(RecordSymbolDeclarationNode recordDeclaration)
    {
        ImmutableArray<PseudoPropertySymbol>.Builder properties = ImmutableArray.CreateBuilder<PseudoPropertySymbol>();

        foreach (PropertyDeclarationNode propertyDeclaration in recordDeclaration.Properties)
        {
            properties.Add(new PseudoPropertySymbol
            {
                Name = propertyDeclaration.Name,
                Type = propertyDeclaration.Type,
                Index = propertyDeclaration.Index,
            });
        }

        return new PseudoRecordTypeSymbol
        {
            Name = recordDeclaration.Name,
            Properties = properties.ToImmutable(),
            Index = recordDeclaration.Index,
        };
    }

    private static PseudoUnionTypeSymbol CreateUnionSymbolFromDeclaration(UnionTypeSymbolDeclarationNode unionDeclaration)
    {
        return new PseudoUnionTypeSymbol
        {
            Name = unionDeclaration.Name,
            Subtypes = unionDeclaration.Subtypes,
            Index = unionDeclaration.Index,
        };
    }

    private static TypeSymbol GetTypeSymbol(TypeNode typeNode, SymbolTable table)
    {
        switch (typeNode)
        {
            case IdentifierTypeNode { Identifier: string identifier }:
                // we already bound so we can safely cast here
                return (TypeSymbol)table[identifier];
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private static bool TypesAreCompatible(TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (targetType is UnionTypeSymbol union)
        {
            foreach (TypeSymbol subtype in union.Subtypes)
            {
                if (TypesAreCompatible(sourceType, subtype))
                {
                    return true;
                }
            }

            return false;
        }

        return sourceType.Index == targetType.Index;
    }
}
