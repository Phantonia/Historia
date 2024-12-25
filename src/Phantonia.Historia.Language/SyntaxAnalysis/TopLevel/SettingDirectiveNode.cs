using Phantonia.Historia.Language.LexicalAnalysis;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public abstract record SettingDirectiveNode() : TopLevelNode
{
    public required Token SettingKeywordToken { get; init; }

    public required Token SettingNameToken { get; init; }

    public string SettingName => SettingNameToken.Text;

    public required Token ColonToken { get; init; }

    public required Token SemicolonToken { get; init; }

    protected abstract IReconstructable GetValue();

    protected override void ReconstructCore(TextWriter writer)
    {
        SettingKeywordToken.Reconstruct(writer);
        SettingNameToken.Reconstruct(writer);
        ColonToken.Reconstruct(writer);
        GetValue().Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }
}
