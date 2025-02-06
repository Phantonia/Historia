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
        List<CompilationUnitNode> compilationUnits = [.. story.CompilationUnits];

        for (int i = 0; i < story.CompilationUnits.Length; i++)
        {
            CompilationUnitNode compilationUnit = story.CompilationUnits[i];

            foreach (TopLevelNode declaration in compilationUnit.TopLevelNodes)
            {
                (table, TopLevelNode boundDeclaration) = BindPseudoDeclaration(declaration, table);
                compilationUnit = compilationUnit with
                {
                    TopLevelNodes = compilationUnit.TopLevelNodes.Replace(declaration, boundDeclaration),
                };
            }

            compilationUnits[i] = compilationUnit;

        }

        StoryNode boundStory = story with
        {
            CompilationUnits = [.. compilationUnits],
        };

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
            NameToken = referenceDeclaration.NameToken,
            Symbol = table[referenceDeclaration.Name],
            Index = referenceDeclaration.Index,
            PrecedingTokens = [],
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
            NameToken = interfaceDeclaration.NameToken,
            Original = interfaceDeclaration with
            {
                Methods = [.. methods],
            },
            Symbol = table[interfaceDeclaration.Name],
            Index = interfaceDeclaration.Index,
            PrecedingTokens = [],
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, InterfaceMethodDeclarationNode) BindPseudoInterfaceMethodDeclaration(InterfaceMethodDeclarationNode methodDeclaration, SymbolTable table)
    {
        List<ParameterDeclarationNode> parameters = [.. methodDeclaration.Parameters];

        for (int i = 0; i < parameters.Count; i++)
        {
            BindingContext context = new()
            {
                SymbolTable = table,
            };

            (_, TypeNode boundType) = BindType(parameters[i].Type, context);

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
            BindingContext context = new()
            {
                SymbolTable = table,
            };

            (context, TypeNode boundType) = BindType(properties[i].Type, context);

            properties[i] = properties[i] with
            {
                Type = boundType,
            };
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            NameToken = recordDeclaration.NameToken,
            Original = recordDeclaration with
            {
                Properties = [.. properties],
            },
            Symbol = table[recordDeclaration.Name],
            Index = recordDeclaration.Index,
            PrecedingTokens = [],
        };

        return (table, boundDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindPseudoUnionDeclaration(UnionSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        List<TypeNode> subtypes = [.. unionDeclaration.Subtypes];

        BindingContext context = new()
        {
            SymbolTable = table,
        };

        for (int i = 0; i < subtypes.Count; i++)
        {
            (context, TypeNode boundType) = BindType(subtypes[i], context);
            subtypes[i] = boundType;
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            NameToken = unionDeclaration.NameToken,
            Original = unionDeclaration with
            {
                Subtypes = [.. subtypes],
            },
            Symbol = table[unionDeclaration.Name],
            Index = unionDeclaration.Index,
            PrecedingTokens = [],
        };

        return (context.SymbolTable, boundDeclaration);
    }

    private static (SymbolTable, TopLevelNode) BindPseudoEnumDeclaration(EnumSymbolDeclarationNode enumDeclaration, SymbolTable table)
    {
        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            NameToken = enumDeclaration.NameToken,
            Original = enumDeclaration,
            Symbol = table[enumDeclaration.Name],
            Index = enumDeclaration.Index,
            PrecedingTokens = [],
        };

        return (table, boundDeclaration);
    }
}
