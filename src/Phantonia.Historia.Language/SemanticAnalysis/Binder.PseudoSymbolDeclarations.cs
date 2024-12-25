using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StoryNode) BindPseudoSymbolDeclarations(SymbolTable table)
    {
        StoryNode boundStory = story;

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
            (table, TopLevelNode boundDeclaration) = BindPseudoDeclaration(declaration, table);
            boundStory = boundStory with
            {
                TopLevelNodes = boundStory.TopLevelNodes.Replace(declaration, boundDeclaration),
            };
        }

        return (table, boundStory);
    }

    private (SymbolTable, TopLevelNode) BindPseudoDeclaration(TopLevelNode declaration, SymbolTable table) => declaration switch
    {
        RecordSymbolDeclarationNode recordDeclaration => BindPseudoRecordDeclaration(recordDeclaration, table),
        UnionSymbolDeclarationNode unionDeclaration => BindPseudoUnionDeclaration(unionDeclaration, table),
        EnumSymbolDeclarationNode enumDeclaration => BindPseudoEnumDeclaration(enumDeclaration, table),
        ReferenceSymbolDeclarationNode referenceDeclaration => BindPseudoReferenceDeclaration(referenceDeclaration, table),
        InterfaceSymbolDeclarationNode interfaceDeclaration => BindPseudoInterfaceDeclaration(interfaceDeclaration, table),
        _ => (table, declaration),
    };

    private (SymbolTable, TopLevelNode) BindPseudoReferenceDeclaration(ReferenceSymbolDeclarationNode referenceDeclaration, SymbolTable table)
    {
        if (!table.IsDeclared(referenceDeclaration.InterfaceName))
        {
            ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(referenceDeclaration.InterfaceName, referenceDeclaration.Index));
            return (table, referenceDeclaration);
        }

        Symbol alledgedInterface = table[referenceDeclaration.InterfaceName];

        if (alledgedInterface is not PseudoInterfaceSymbol)
        {
            ErrorFound?.Invoke(Errors.SymbolIsNotInterface(alledgedInterface.Name, referenceDeclaration.Index));
            return (table, referenceDeclaration);
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Original = referenceDeclaration,
            Name = referenceDeclaration.Name,
            Symbol = table[referenceDeclaration.Name],
            Index = referenceDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindPseudoInterfaceDeclaration(InterfaceSymbolDeclarationNode interfaceDeclaration, SymbolTable table)
    {
        List<InterfaceMethodDeclarationNode> methods = [.. interfaceDeclaration.Methods];

        for (int i = 0; i < methods.Count; i++)
        {
            (table, methods[i]) = BindPseudoInterfaceMethodDeclaration(methods[i], table);
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = interfaceDeclaration.Name,
            Original = interfaceDeclaration with
            {
                Methods = [.. methods],
            },
            Symbol = table[interfaceDeclaration.Name],
            Index = interfaceDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, InterfaceMethodDeclarationNode) BindPseudoInterfaceMethodDeclaration(InterfaceMethodDeclarationNode methodDeclaration, SymbolTable table)
    {
        List<ParameterDeclarationNode> parameters = [.. methodDeclaration.Parameters];

        for (int i = 0; i < parameters.Count; i++)
        {
            (table, TypeNode boundType) = BindType(parameters[i].Type, table);
            parameters[i] = parameters[i] with
            {
                Type = boundType,
            };
        }

        methodDeclaration = methodDeclaration with
        {
            Parameters = [.. parameters],
        };

        return (table, methodDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindPseudoRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
    {
        List<ParameterDeclarationNode> properties = [.. recordDeclaration.Properties];

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
            Original = recordDeclaration with
            {
                Properties = [.. properties],
            },
            Symbol = table[recordDeclaration.Name],
            Index = recordDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindPseudoUnionDeclaration(UnionSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        List<TypeNode> subtypes = [.. unionDeclaration.Subtypes];

        for (int i = 0; i < subtypes.Count; i++)
        {
            (table, TypeNode boundType) = BindType(subtypes[i], table);
            subtypes[i] = boundType;
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Original = unionDeclaration with
            {
                Subtypes = [.. subtypes],
            },
            Symbol = table[unionDeclaration.Name],
            Index = unionDeclaration.Index,
        };

        return (table, boundDeclaration);
    }

    private static (SymbolTable, TopLevelNode) BindPseudoEnumDeclaration(EnumSymbolDeclarationNode enumDeclaration, SymbolTable table)
    {
        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = enumDeclaration.Name,
            Original = enumDeclaration,
            Symbol = table[enumDeclaration.Name],
            Index = enumDeclaration.Index,
        };

        return (table, boundDeclaration);
    }
}
