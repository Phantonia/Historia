using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Scope = System.Collections.Immutable.ImmutableDictionary<string, Phantonia.Historia.Language.SemanticAnalysis.Symbols.Symbol>;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record SymbolTable
{
    public SymbolTable() { }

    private ImmutableStack<Scope> Table { get; init; } = [];

    public ImmutableList<Symbol> AllSymbols { get; init; } = [];

    public Symbol this[string name]
    {
        get
        {
            foreach (Scope scope in Table)
            {
                if (scope.TryGetValue(name, out Symbol? symbol))
                {
                    return symbol;
                }
            }

            throw new KeyNotFoundException();
        }
    }

    public bool IsDeclared(string name)
    {
        return Table.Any(s => s.ContainsKey(name));
    }

    public SymbolTable Declare(Symbol symbol)
    {
        if (IsDeclared(symbol.Name))
        {
            throw new ArgumentException($"Name '{symbol.Name}' already exists in this symbol table");
        }

        Scope currentScope = Table.Peek();

        return this with
        {
            AllSymbols = AllSymbols.Add(symbol),
            Table = Table.Pop().Push(currentScope.Add(symbol.Name, symbol)),
        };
    }

    public SymbolTable Replace(string name, Symbol newSymbol)
    {
        // we can only replace on our current scope
        Scope currentScope = Table.Peek().SetItem(name, newSymbol);

        return this with
        {
            Table = Table.Pop().Push(currentScope),
        };
    }

    public SymbolTable OpenScope()
    {
        return this with
        {
            Table = Table.Push(Scope.Empty),
        };
    }

    public SymbolTable CloseScope()
    {
        return this with
        {
            Table = Table.Pop(),
        };
    }
}
