using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StoryNode) BindPseudoSymbolDeclarations(SymbolTable table)
    {
        StoryNode boundStory = story;

        foreach (TopLevelNode declaration in story.TopLevelNodes)
        {
            if (declaration is not TypeSymbolDeclarationNode)
            {
                continue;
            }

            (table, TopLevelNode boundDeclaration) = BindPseudoDeclaration(declaration, table);
            boundStory = boundStory with
            {
                TopLevelNodes = boundStory.TopLevelNodes.Replace(declaration, boundDeclaration),
            };
        }

        return (table, boundStory);
    }

    private (SymbolTable, TopLevelNode) BindPseudoDeclaration(TopLevelNode declaration, SymbolTable table)
    {
        switch (declaration)
        {
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindPseudoRecordDeclaration(recordDeclaration, table);
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                return BindPseudoUnionDeclaration(unionDeclaration, table);
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, TopLevelNode) BindPseudoRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
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

    private (SymbolTable, TopLevelNode) BindPseudoUnionDeclaration(UnionTypeSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        List<TypeNode> subtypes = unionDeclaration.Subtypes.ToList();

        for (int i = 0; i < subtypes.Count; i++)
        {
            (table, TypeNode boundType) = BindType(subtypes[i], table);
            subtypes[i] = boundType;
        }

        BoundSymbolDeclarationNode boundDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Declaration = unionDeclaration with
            {
                Subtypes = subtypes.ToImmutableArray(),
            },
            Symbol = table[unionDeclaration.Name],
            Index = unionDeclaration.Index,
        };

        return (table, boundDeclaration);
    }
}
