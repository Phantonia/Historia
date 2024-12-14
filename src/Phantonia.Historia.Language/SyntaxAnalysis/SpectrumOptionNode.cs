using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record SpectrumOptionNode() : SyntaxNode
{
    public required Token NameToken { get; init; }

    public required Token InequalitySignToken { get; init; }

    public required Token NumeratorToken { get; init; }

    public required Token SlashToken { get; init; }

    public required Token DenominatorToken { get; init; }

    public required Token? CommaToken { get; init; }

    public string Name => NameToken.Text;

    public bool Inclusive => InequalitySignToken.Kind == TokenKind.LessThanOrEquals;

    public int Numerator => (int)NumeratorToken.IntegerValue!;

    public int Denominator => (int)DenominatorToken.IntegerValue!;

    public override IEnumerable<SyntaxNode> Children => [];

    public override string ReconstructCore()
        => NameToken.Reconstruct() + InequalitySignToken.Reconstruct() + NumeratorToken.Reconstruct() + SlashToken.Reconstruct() + DenominatorToken.Reconstruct() + CommaToken?.Reconstruct();

    public override void ReconstructCore(TextWriter writer)
    {
        writer.Write(NameToken.Reconstruct());
        writer.Write(InequalitySignToken.Reconstruct());
        writer.Write(NumeratorToken.Reconstruct());
        writer.Write(SlashToken.Reconstruct());
        writer.Write(DenominatorToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"spectrum option {Name} <{(Inclusive ? "=" : "")} {Numerator}/{Denominator}";
}
