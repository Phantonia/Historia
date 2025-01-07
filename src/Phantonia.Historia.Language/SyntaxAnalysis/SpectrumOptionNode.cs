using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed record SpectrumOptionNode() : SyntaxNode
{
    public required Token NameToken { get; init; }

    public required Token? InequalitySignToken { get; init; }

    public required Token? NumeratorToken { get; init; }

    public required Token? SlashToken { get; init; }

    public required Token? DenominatorToken { get; init; }

    public required Token? CommaToken { get; init; }

    public string Name => NameToken.Text;

    public bool Inclusive => (InequalitySignToken?.Kind ?? TokenKind.LessThanOrEquals) is TokenKind.LessThanOrEquals;

    public int Numerator => NumeratorToken?.IntegerValue ?? 1;

    public int Denominator => DenominatorToken?.IntegerValue ?? 1;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        NameToken.Reconstruct(writer);
        InequalitySignToken?.Reconstruct(writer);
        NumeratorToken?.Reconstruct(writer);
        SlashToken?.Reconstruct(writer);
        DenominatorToken?.Reconstruct(writer);
        CommaToken?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"spectrum option {Name} <{(Inclusive ? "=" : "")} {Numerator}/{Denominator}";
}
