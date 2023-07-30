﻿using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.GrammaticalAnalysis.Types;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.Immutable;
using System;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private static SymbolTable FixPseudoSymbols(DependencyGraph dependencyGraph, SymbolTable table)
    {
        IEnumerable<int>? topologicalOrdering = dependencyGraph.TopologicalSort();

        foreach (Symbol symbol in topologicalOrdering.Select(i => dependencyGraph.Symbols[i]))
        {
            TypeSymbol? typeSymbol = symbol as TypeSymbol;
            Debug.Assert(typeSymbol is not null);

            TypeSymbol trueTypeSymbol = TurnIntoTrueTypeSymbol(typeSymbol, table);

            table = table.Replace(trueTypeSymbol.Name, trueTypeSymbol);
        }

        return table;
    }

    private DependencyGraph? BuildTypeDependencyGraph(StoryNode story, SymbolTable table)
    {
        Dictionary<int, Symbol> symbols = new();
        Dictionary<int, IReadOnlyList<int>> dependencies = new();

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
            //Debug.Assert(declaration is SettingSymbolDeclarationNode or BoundSymbolDeclarationNode);

            if (declaration is not BoundSymbolDeclarationNode { Declaration: TypeSymbolDeclarationNode typeDeclaration, Symbol: Symbol symbol })
            {
                continue;
            }

            symbols[symbol.Index] = symbol;
            dependencies[symbol.Index] = GetDependencies(typeDeclaration, table);
        }

        DependencyGraph dependencyGraph = new()
        {
            Symbols = symbols,
            Dependencies = dependencies,
        };

        if (dependencyGraph.IsCyclic(out IEnumerable<int>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicTypeDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));

            return null;
        }

        return dependencyGraph;
    }

    private static IReadOnlyList<int> GetDependencies(TypeSymbolDeclarationNode declaration, SymbolTable table)
    {
        List<int> dependencies = new();

        switch (declaration)
        {
            case RecordSymbolDeclarationNode recordDeclaration:
                foreach (PropertyDeclarationNode propertyDeclaration in recordDeclaration.Properties)
                {
                    Debug.Assert(propertyDeclaration.Type is BoundTypeNode);

                    switch (((BoundTypeNode)propertyDeclaration.Type).Node)
                    {
                        case IdentifierTypeNode { Identifier: string identifier }:
                            if (table[identifier] is not BuiltinTypeSymbol)
                            {
                                // we don't need dependencies on built in type symbols as they can never reference user defined type symbols
                                dependencies.Add(table[identifier].Index);
                            }

                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                break;
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                foreach (TypeNode subtype in unionDeclaration.Subtypes)
                {
                    if (((BoundTypeNode)subtype).Node is IdentifierTypeNode { Identifier: string identifier })
                    {
                        if (table[identifier] is not BuiltinTypeSymbol)
                        {
                            dependencies.Add(table[identifier].Index);
                        }
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                break;
            default:
                Debug.Assert(false);
                break;
        }

        return dependencies;
    }

    private static TypeSymbol TurnIntoTrueTypeSymbol(TypeSymbol typeSymbol, SymbolTable table)
    {
        switch (typeSymbol)
        {
            case PseudoRecordTypeSymbol recordSymbol:
                return TurnIntoTrueRecordSymbol(recordSymbol, table);
            case PseudoUnionTypeSymbol unionSymbol:
                return TurnIntoTrueUnionSymbol(unionSymbol, table);
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private static RecordTypeSymbol TurnIntoTrueRecordSymbol(PseudoRecordTypeSymbol pseudoRecord, SymbolTable table)
    {
        ImmutableArray<PropertySymbol>.Builder trueProperties = ImmutableArray.CreateBuilder<PropertySymbol>(initialCapacity: pseudoRecord.Properties.Length);

        foreach (PseudoPropertySymbol pseudoProperty in pseudoRecord.Properties)
        {
            trueProperties.Add(TurnIntoTruePropertySymbol(pseudoProperty, table));
        }

        return new RecordTypeSymbol
        {
            Name = pseudoRecord.Name,
            Properties = trueProperties.MoveToImmutable(),
            Index = pseudoRecord.Index,
        };
    }

    private static PropertySymbol TurnIntoTruePropertySymbol(PseudoPropertySymbol pseudoProperty, SymbolTable table)
    {
        return new PropertySymbol
        {
            Name = pseudoProperty.Name,
            Type = GetTypeSymbol(pseudoProperty.Type, table),
            Index = pseudoProperty.Index,
        };
    }

    private static UnionTypeSymbol TurnIntoTrueUnionSymbol(PseudoUnionTypeSymbol pseudoUnion, SymbolTable table)
    {
        HashSet<TypeSymbol> trueSubtypes = new();

        foreach (TypeNode subtype in pseudoUnion.Subtypes)
        {
            TypeSymbol subtypeSymbol = GetTypeSymbol(subtype, table);

            if (subtypeSymbol is UnionTypeSymbol subtypeUnion)
            {
                foreach (TypeSymbol subsubtypes in subtypeUnion.Subtypes)
                {
                    trueSubtypes.Add(subsubtypes);
                }
            }
            else
            {
                trueSubtypes.Add(GetTypeSymbol(subtype, table));
            }
        }

        return new UnionTypeSymbol
        {
            Name = pseudoUnion.Name,
            Subtypes = trueSubtypes.ToImmutableArray(),
            Index = pseudoUnion.Index,
        };
    }
}
