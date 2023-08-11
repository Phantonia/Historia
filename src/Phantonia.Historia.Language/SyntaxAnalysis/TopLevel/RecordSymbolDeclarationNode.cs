using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record RecordSymbolDeclarationNode : TypeSymbolDeclarationNode
{
    public RecordSymbolDeclarationNode() { }

    public required ImmutableArray<PropertyDeclarationNode> Properties { get; init; }

    public override IEnumerable<SyntaxNode> Children => Properties;

    protected internal override string GetDebuggerDisplay() => $"declare record {Name} {{ {string.Join(", ", Properties.Select(p => p.GetDebuggerDisplay()))} }}";
}
