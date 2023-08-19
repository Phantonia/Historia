using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, TopLevelNode) BindTopLevelNode(TopLevelNode declaration, Settings settings, SymbolTable table)
    {
        if (declaration is BoundSymbolDeclarationNode { Declaration: var innerDeclaration })
        {
            return BindTopLevelNode(innerDeclaration, settings, table);
        }

        switch (declaration)
        {
            case SceneSymbolDeclarationNode sceneDeclaration:
                return BindSceneDeclaration(sceneDeclaration, settings, table);
            case RecordSymbolDeclarationNode recordDeclaration:
                return BindRecordDeclaration(recordDeclaration, table);
            case UnionTypeSymbolDeclarationNode unionDeclaration:
                return BindUnionDeclaration(unionDeclaration, table);
            case OutcomeSymbolDeclarationNode outcomeDeclaration:
                return (table, new BoundSymbolDeclarationNode
                {
                    Declaration = outcomeDeclaration,
                    Symbol = table[outcomeDeclaration.Name],
                    Name = outcomeDeclaration.Name,
                    Index = outcomeDeclaration.Index,
                });
            case SpectrumSymbolDeclarationNode spectrumDeclaration:
                return (table, new BoundSymbolDeclarationNode
                {
                    Declaration = spectrumDeclaration,
                    Symbol = table[spectrumDeclaration.Name],
                    Name = spectrumDeclaration.Name,
                    Index = spectrumDeclaration.Index,
                });
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, BoundSymbolDeclarationNode) BindRecordDeclaration(RecordSymbolDeclarationNode recordDeclaration, SymbolTable table)
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

    private (SymbolTable, TopLevelNode) BindUnionDeclaration(UnionTypeSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(unionDeclaration.Name) && table[unionDeclaration.Name] is UnionTypeSymbol);

        UnionTypeSymbol unionSymbol = (UnionTypeSymbol)table[unionDeclaration.Name];

        BoundSymbolDeclarationNode boundUnionDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Declaration = unionDeclaration,
            Symbol = unionSymbol,
            Index = unionDeclaration.Index,
        };

        return (table, boundUnionDeclaration);
    }

    private (SymbolTable, BoundSymbolDeclarationNode) BindSceneDeclaration(SceneSymbolDeclarationNode sceneDeclaration, Settings settings, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(sceneDeclaration.Name) && table[sceneDeclaration.Name] is SceneSymbol);

        SceneSymbol sceneSymbol = (SceneSymbol)table[sceneDeclaration.Name];

        (table, StatementBodyNode boundBody) = BindStatementBody(sceneDeclaration.Body, settings, table);

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
}
