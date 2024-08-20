using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Generic;
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
            case UnionSymbolDeclarationNode unionDeclaration:
                return BindUnionDeclaration(unionDeclaration, table);
            case EnumSymbolDeclarationNode enumDeclaration:
                return BindEnumDeclaration(enumDeclaration, table);
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

        string[] bannedMemberNames =
        [
            recordDeclaration.Name,
            nameof(Equals),
            nameof(GetHashCode),
            nameof(GetType),
            nameof(MemberwiseClone), // if anyone ever names a record property 'MemberwiseClone', i'll be very impressed, but better be prepared for everything
            nameof(ReferenceEquals),
            nameof(ToString),
            "op_Equality",
            "op_Inequality",
        ];

        HashSet<string> propertyNames = [];

        foreach ((PropertyDeclarationNode propertyDeclaration, PropertySymbol propertySymbol) in recordDeclaration.Properties.Zip(recordSymbol.Properties))
        {
            Debug.Assert(propertyDeclaration.Name == propertySymbol.Name);
            Debug.Assert(propertyDeclaration.Index == propertySymbol.Index);

            // spec 1.2.1.2: "Due to technical reasons, no property of a record may have any of the following names: [...]"
            if (bannedMemberNames.Contains(propertyDeclaration.Name))
            {
                ErrorFound?.Invoke(Errors.ConflictingRecordProperty(recordDeclaration.Name, propertyDeclaration.Name, propertyDeclaration.Index));
            }

            // spec 1.2.1.2: "All the property names [in a record] must be different."
            if (!propertyNames.Add(propertyDeclaration.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedRecordPropertyName(recordSymbol.Name, propertyDeclaration.Name, propertyDeclaration.Index));
            }

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

    private (SymbolTable, TopLevelNode) BindUnionDeclaration(UnionSymbolDeclarationNode unionDeclaration, SymbolTable table)
    {
        Debug.Assert(table.IsDeclared(unionDeclaration.Name) && table[unionDeclaration.Name] is UnionTypeSymbol);

        UnionTypeSymbol unionSymbol = (UnionTypeSymbol)table[unionDeclaration.Name];

        string[] bannedMemberNames =
        [
            unionDeclaration.Name,
            "Discriminator",
            "Run",
            "Evaluate",
            "AsObject",
            nameof(Equals),
            nameof(GetHashCode),
            nameof(GetType),
            nameof(MemberwiseClone),
            nameof(ReferenceEquals),
            nameof(ToString),
            "op_Equality",
            "op_Inequality",
            $"{unionDeclaration.Name}Discriminator",
        ];

        foreach (TypeNode type in unionDeclaration.Subtypes)
        {
            Debug.Assert(type is BoundTypeNode);

            string identifier = ((IdentifierTypeNode)((BoundTypeNode)type).Node).Identifier;

            // spec 1.2.1.4: "Due to technical reasons, no subtype of a union may have any of the following names: [...]"
            if (bannedMemberNames.Contains(identifier))
            {
                ErrorFound?.Invoke(Errors.ConflictingUnionSubtype(unionDeclaration.Name, identifier, type.Index));
            }
        }

        BoundSymbolDeclarationNode boundUnionDeclaration = new()
        {
            Name = unionDeclaration.Name,
            Declaration = unionDeclaration,
            Symbol = unionSymbol,
            Index = unionDeclaration.Index,
        };

        return (table, boundUnionDeclaration);
    }

    private (SymbolTable, TopLevelNode) BindEnumDeclaration(EnumSymbolDeclarationNode enumDeclaration, SymbolTable table)
    {
        HashSet<string> options = [];

        foreach (string option in enumDeclaration.Options)
        {
            // spec 1.2.1.3: "Each option [in an enum] must have a different name."
            if (!options.Add(option))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInEnum(enumDeclaration.Name, option, enumDeclaration.Index));
            }
        }

        EnumTypeSymbol symbol = (EnumTypeSymbol)table[enumDeclaration.Name];

        BoundSymbolDeclarationNode boundEnumDeclaration = new()
        {
            Name = enumDeclaration.Name,
            Declaration = enumDeclaration,
            Symbol = symbol,
            Index = enumDeclaration.Index,
        };

        return (table, boundEnumDeclaration);
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
