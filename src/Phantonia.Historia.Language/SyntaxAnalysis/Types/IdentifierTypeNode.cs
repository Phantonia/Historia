using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Types;

public sealed record IdentifierTypeNode() : TypeNode
{
    public required Token IdentifierToken { get; init; }

    public string Identifier => IdentifierToken.Text;

    protected override void ReconstructCore(TextWriter writer) => IdentifierToken.Reconstruct(writer);

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"type {Identifier}";
}
