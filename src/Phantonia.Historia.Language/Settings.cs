using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public sealed record Settings
{
    static Settings()
    {
        AllSettings = TypeSettings.Union(ExpressionSettings);
    }

    private static readonly TypeSymbol IntType = new BuiltinTypeSymbol
    {
        Name = "Int",
        Type = BuiltinType.Int,
        Index = Constants.IntTypeIndex,
    };

    public static ImmutableHashSet<string> AllSettings { get; }

    public static ImmutableHashSet<string> TypeSettings { get; } = new[]
    {
        nameof(OutputType),
        nameof(OptionType),
    }.ToImmutableHashSet();

    public static ImmutableHashSet<string> ExpressionSettings { get; } = new[]
    {
        nameof(Namespace),
        nameof(StoryName),
    }.ToImmutableHashSet();

    public Settings() { }

    public string Namespace { get; init; } = ""; // global namespace

    public string StoryName { get; init; } = "HistoriaStory";

    public TypeSymbol OutputType { get; init; } = IntType;

    public TypeSymbol OptionType { get; init; } = IntType;
}
