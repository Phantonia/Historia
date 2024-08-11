using System;

namespace Phantonia.Historia.Language.FlowAnalysis;

[Flags]
public enum FlowEdgeKind
{
    Story = 1 << 0,
    Semantic = 1 << 1,

    Weak = Story,
    Strong = Story | Semantic,
}
