using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public record PropertyDeclarationNode : SyntaxNode
{
    public PropertyDeclarationNode() { }

    public required string Name { get; init; }

    public required TypeNode Type { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Type };

    protected internal override string GetDebuggerDisplay() => $"property {Name} of type {Type.GetDebuggerDisplay()}";
}
