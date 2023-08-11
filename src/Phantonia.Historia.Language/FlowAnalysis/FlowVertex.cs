using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Diagnostics;

namespace Phantonia.Historia.Language.FlowAnalysis;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly record struct FlowVertex
{
    public required StatementNode AssociatedStatement { get; init; }

    public required int Index { get; init; }

    public required bool IsVisible { get; init; }

    private string GetDebuggerDisplay() => $"{(IsVisible ? "" : "in")}visible flow vertex w/ statement ({AssociatedStatement.GetDebuggerDisplay()}) @ index {Index}";
}
