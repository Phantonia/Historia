using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

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

    public Binder(IEnumerable<StoryNode> stories)
    {
        story = new StoryNode
        {
            Index = 0,
            TopLevelNodes = stories.SelectMany(s => s.TopLevelNodes).ToImmutableArray(),
        };
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

        // 6. bind whole tree
        (table, StoryNode boundStory) = BindTree(halfboundStory, settings, table);

        return new BindingResult(boundStory, settings, table);
    }

    private static SymbolTable GetBuiltinSymbolTable()
    {
        // spec 1.2.1.1
        SymbolTable symbolTable = new();
        symbolTable = symbolTable.OpenScope()
                                 .Declare(new BuiltinTypeSymbol { Name = "Int", Type = BuiltinType.Int, Index = -1 })
                                 .Declare(new BuiltinTypeSymbol { Name = "String", Type = BuiltinType.String, Index = -2 })
                                 .Declare(new BuiltinTypeSymbol { Name = "Boolean", Type = BuiltinType.Boolean, Index = -3 });
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

    private Symbol? CreateSymbolFromDeclaration(TopLevelNode declaration)
    {
        switch (declaration)
        {
            case SceneSymbolDeclarationNode { Name: string name, Index: int index }:
                return new SceneSymbol { Name = name, Index = index };
            case RecordSymbolDeclarationNode recordDeclaration:
                return CreateRecordSymbolFromDeclaration(recordDeclaration);
            case UnionSymbolDeclarationNode unionDeclaration:
                return CreateUnionSymbolFromDeclaration(unionDeclaration);
            case EnumSymbolDeclarationNode enumDeclaration:
                return CreateEnumSymbolFromDeclaration(enumDeclaration);
            case OutcomeSymbolDeclarationNode outcomeDeclaration:
                return CreateOutcomeSymbolFromDeclaration(outcomeDeclaration);
            case SpectrumSymbolDeclarationNode spectrumDeclaration:
                return CreateSpectrumSymbolFromDeclaration(spectrumDeclaration);
            case ReferenceSymbolDeclarationNode referenceDeclaration:
                return CreateReferenceSymbolFromDeclaration(referenceDeclaration);
            case InterfaceSymbolDeclarationNode interfaceDeclaration:
                return CreateInterfaceSymbolFromDeclaration(interfaceDeclaration);
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

        foreach (ParameterDeclarationNode propertyDeclaration in recordDeclaration.Properties)
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

    private static PseudoUnionTypeSymbol CreateUnionSymbolFromDeclaration(UnionSymbolDeclarationNode unionDeclaration)
    {
        return new PseudoUnionTypeSymbol
        {
            Name = unionDeclaration.Name,
            Subtypes = unionDeclaration.Subtypes,
            Index = unionDeclaration.Index,
        };
    }

    private static PseudoEnumTypeSymbol CreateEnumSymbolFromDeclaration(EnumSymbolDeclarationNode enumDeclaration)
    {
        return new PseudoEnumTypeSymbol
        {
            Name = enumDeclaration.Name,
            Options = enumDeclaration.Options,
            Index = enumDeclaration.Index,
        };
    }

    private OutcomeSymbol? CreateOutcomeSymbolFromDeclaration(IOutcomeDeclarationNode outcomeDeclaration)
    {
        bool error = false;

        HashSet<string> optionNames = [];

        foreach (string option in outcomeDeclaration.Options)
        {
            // spec 1.2.3.1: "A classic outcome is an outcome which may take on one of finitely many (including zero) named options. All these options have be different."
            if (!optionNames.Add(option))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option, outcomeDeclaration.Index));
                error = true;
            }
        }

        if (outcomeDeclaration.DefaultOption is not null && !optionNames.Contains(outcomeDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        if (error)
        {
            return null;
        }

        return new OutcomeSymbol()
        {
            Name = outcomeDeclaration.Name,
            OptionNames = [.. optionNames],
            DefaultOption = outcomeDeclaration.DefaultOption,
            AlwaysAssigned = false,
            IsPublic = outcomeDeclaration.IsPublic,
            Index = outcomeDeclaration.Index,
        };
    }

    private SpectrumSymbol? CreateSpectrumSymbolFromDeclaration(ISpectrumDeclarationNode spectrumDeclaration)
    {
        bool error = false;

        // spec 1.2.3.2.: "These intervals partition [0, 1], i.e. are all pairwise disjoint, non-empty and their union is exactly the interval [0, 1]."
        // a partition cannot be empty, as the empty union is the empty set and not the interval [0, 1]
        if (spectrumDeclaration.Options.Length == 0)
        {
            ErrorFound?.Invoke(Errors.OutcomeWithZeroOptions(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        HashSet<string> optionNames = [];

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            // spec 1.2.3.2: "[A] spectrum [...] defines named options where all of these names have to be different."
            if (!optionNames.Add(option.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option.Name, spectrumDeclaration.Index));
                error = true;
            }

            if (option.Denominator == 0)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundDivisionByZero(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            // spec 1.2.3.2: "These options are assigned intervals I ⊆ [0, 1] [...]."
            // that is, the numerator may not be greater than the denominator
            else if (option.Numerator > option.Denominator)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundNotInRange(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            // spec 1.2.3.2: "These options are assigned intervals I ⊆ [0, 1] [...]."
            // that is, the numerator and denominator must both be positive
            else if (option.Numerator < 0 || option.Denominator < 0)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundNotInRange(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
        }

        // spec 1.2.3.2: "A spectrum may or may not have a default option, which in that case has to be one of the listed options."
        if (spectrumDeclaration.DefaultOption is not null && !optionNames.Contains(spectrumDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        if (spectrumDeclaration.Options.Length == 0)
        {
            Debug.Assert(error);
            return null;
        }

        if (error)
        {
            return null;
        }

        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        static int Lcm(int a, int b)
        {
            return a * b / Gcd(a, b);
        }

        int commonDenominator = 1;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            commonDenominator = Lcm(commonDenominator, option.Denominator);
        }

        Debug.Assert(commonDenominator != 0);

        ImmutableDictionary<string, SpectrumInterval>.Builder intervalBuilder = ImmutableDictionary.CreateBuilder<string, SpectrumInterval>();

        (int, int) previousFraction = (0, 1);
        int previousNumerator = 0;
        bool previousInclusive = true;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            // let x be the common denominator
            // then a fraction a/b = (a * (x/b))/x
            // x/b is an integer by definition of lcm
            SpectrumInterval interval = new()
            {
                Inclusive = option.Inclusive,
                UpperDenominator = commonDenominator,
                UpperNumerator = option.Numerator * commonDenominator / option.Denominator,
            };

            if (interval.UpperNumerator <= previousNumerator)
            {
                // this is only okay if the previous one was exclusive and this one is inclusive
                if (previousInclusive || !option.Inclusive)
                {
                    ErrorFound?.Invoke(Errors.SpectrumNotIncreasing(spectrumDeclaration.Name, previousFraction, (option.Numerator, option.Denominator), option.Index));
                    error = true;
                }
            }

            intervalBuilder.Add(option.Name, interval);

            previousFraction = (option.Numerator, option.Denominator);
            previousNumerator = interval.UpperNumerator;
            previousInclusive = interval.Inclusive;
        }

        return new SpectrumSymbol()
        {
            Name = spectrumDeclaration.Name,
            Intervals = intervalBuilder.ToImmutable(),
            OptionNames = spectrumDeclaration.Options.Select(o => o.Name).ToImmutableArray(),
            DefaultOption = spectrumDeclaration.DefaultOption,
            AlwaysAssigned = false,
            IsPublic = spectrumDeclaration.IsPublic,
            Index = spectrumDeclaration.Index,
        };
    }

    private static PseudoReferenceSymbol CreateReferenceSymbolFromDeclaration(ReferenceSymbolDeclarationNode referenceDeclaration)
    {
        return new PseudoReferenceSymbol
        {
            Name = referenceDeclaration.Name,
            InterfaceName = referenceDeclaration.InterfaceName,
            Index = referenceDeclaration.Index,
        };
    }

    private PseudoInterfaceSymbol CreateInterfaceSymbolFromDeclaration(InterfaceSymbolDeclarationNode interfaceDeclaration)
    {
        ImmutableArray<PseudoInterfaceMethodSymbol>.Builder methods = ImmutableArray.CreateBuilder<PseudoInterfaceMethodSymbol>();
        HashSet<string> methodNames = new(capacity: interfaceDeclaration.Methods.Length);

        foreach (InterfaceMethodDeclarationNode methodDeclaration in interfaceDeclaration.Methods)
        {
            if (!methodNames.Add(methodDeclaration.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedMethodNameInInterface(interfaceDeclaration.Name, methodDeclaration.Name, methodDeclaration.Index));
            }

            ImmutableArray<PseudoPropertySymbol>.Builder parameters = ImmutableArray.CreateBuilder<PseudoPropertySymbol>();

            foreach (ParameterDeclarationNode propertyDeclaration in methodDeclaration.Parameters)
            {
                parameters.Add(new PseudoPropertySymbol
                {
                    Name = propertyDeclaration.Name,
                    Type = propertyDeclaration.Type,
                    Index = propertyDeclaration.Index,
                });
            }

            methods.Add(new PseudoInterfaceMethodSymbol
            {
                Kind = methodDeclaration.Kind,
                Name = methodDeclaration.Name,
                Parameters = parameters.ToImmutable(),
                Index = methodDeclaration.Index,
            });
        }

        return new PseudoInterfaceSymbol
        {
            Name = interfaceDeclaration.Name,
            Methods = methods.ToImmutable(),
            Index = interfaceDeclaration.Index,
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
