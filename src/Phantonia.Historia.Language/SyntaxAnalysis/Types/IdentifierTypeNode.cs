using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Types;

public sealed record IdentifierTypeNode() : TypeNode
{
    public required Token IdentifierToken { get; init; }

    public string Identifier => IdentifierToken.Text;

    public override string Reconstruct() => IdentifierToken.Reconstruct();

    public override void Reconstruct(TextWriter writer) => writer.Write(IdentifierToken.Reconstruct());

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"type {Identifier}";
}
