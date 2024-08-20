using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private DependencyGraph? BuildTypeDependencyGraph(StoryNode story, SymbolTable table)
    {
        Dictionary<int, Symbol> symbols = [];
        Dictionary<int, IReadOnlyList<int>> dependencies = [];

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
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

        // spec 1.2.1.5: "No type may ever directly or indirectly depend on itself."
        if (dependencyGraph.IsCyclic(out IEnumerable<int>? cycle))
        {
            ErrorFound?.Invoke(Errors.CyclicTypeDefinition(cycle.Select(i => dependencyGraph.Symbols[i].Name), dependencyGraph.Symbols[cycle.First()].Index));

            return null;
        }

        return dependencyGraph;
    }

    private static IReadOnlyList<int> GetDependencies(TypeSymbolDeclarationNode declaration, SymbolTable table)
    {
        List<int> dependencies = [];

        switch (declaration)
        {
            case RecordSymbolDeclarationNode recordDeclaration:
                // spec 1.2.1.5: "A type A is directly depends on another type B, if A is a record and B is the type of any of its properties [...]"
                foreach (PropertyDeclarationNode propertyDeclaration in recordDeclaration.Properties)
                {
                    Debug.Assert(propertyDeclaration.Type is BoundTypeNode);

                    switch (((BoundTypeNode)propertyDeclaration.Type).Node)
                    {
                        case IdentifierTypeNode { Identifier: string identifier }:
                            // we don't need dependencies on built in type symbols as they can never reference user defined type symbols
                            if (table[identifier] is not BuiltinTypeSymbol)
                            {
                                dependencies.Add(table[identifier].Index);
                            }

                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
                break;
            case UnionSymbolDeclarationNode unionDeclaration:
                // spec 1.2.1.5: "A type A is directly depends on another type B, [if] [...] A is a union and B is a subtype of this union"
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
            case EnumSymbolDeclarationNode:
                // no dependencies at all
                break;
            default:
                Debug.Assert(false);
                break;
        }

        return dependencies;
    }

    private SymbolTable FixPseudoSymbols(DependencyGraph dependencyGraph, SymbolTable table)
    {
        IEnumerable<int> order = dependencyGraph.GetDependencyRespectingOrder();

        foreach (Symbol symbol in order.Select(i => dependencyGraph.Symbols[i]))
        {
            TypeSymbol? typeSymbol = symbol as TypeSymbol;
            Debug.Assert(typeSymbol is not null);

            TypeSymbol trueTypeSymbol = TurnIntoTrueTypeSymbol(typeSymbol, table);

            table = table.Replace(trueTypeSymbol.Name, trueTypeSymbol);
        }

        return table;
    }

    private TypeSymbol TurnIntoTrueTypeSymbol(TypeSymbol typeSymbol, SymbolTable table)
    {
        switch (typeSymbol)
        {
            case PseudoRecordTypeSymbol recordSymbol:
                return TurnIntoTrueRecordSymbol(recordSymbol, table);
            case PseudoUnionTypeSymbol unionSymbol:
                return TurnIntoTrueUnionSymbol(unionSymbol, table);
            case EnumTypeSymbol enumSymbol:
                return enumSymbol;
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

    private UnionTypeSymbol TurnIntoTrueUnionSymbol(PseudoUnionTypeSymbol pseudoUnion, SymbolTable table)
    {
        HashSet<TypeSymbol> listedSubtypes = [];

        foreach (TypeNode subtype in pseudoUnion.Subtypes)
        {
            TypeSymbol subtypeSymbol = GetTypeSymbol(subtype, table);

            if (!listedSubtypes.Add(subtypeSymbol))
            {
                ErrorFound?.Invoke(Errors.UnionHasDuplicateSubtype(pseudoUnion.Name, subtypeSymbol.Name, pseudoUnion.Index));
            }
        }

        // spec 1.2.1.4: "Unions flatten their subtypes."
        // let U be the union of A and B, where B is the union of C and D
        // then U really is the union of A, C and D
        Queue<TypeSymbol> subtypeQueue = new(listedSubtypes);
        HashSet<TypeSymbol> trueSubtypes = [];

        while (subtypeQueue.TryDequeue(out TypeSymbol? subtype))
        {
            if (subtype is UnionTypeSymbol unionSubtype)
            {
                foreach (TypeSymbol subsubtype in unionSubtype.Subtypes)
                {
                    subtypeQueue.Enqueue(subsubtype);
                }
            }
            else
            {
                trueSubtypes.Add(subtype);
            }
        }

        return new UnionTypeSymbol
        {
            Name = pseudoUnion.Name,
            Subtypes = [.. trueSubtypes],
            Index = pseudoUnion.Index,
        };
    }
}
