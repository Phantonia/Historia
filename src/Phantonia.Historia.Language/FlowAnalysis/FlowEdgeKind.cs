using System;

namespace Phantonia.Historia.Language.FlowAnalysis;

[Flags]
public enum FlowEdgeKind
{
    None = 0,

    // a story edge is reflected in the state machine
    Story = 1 << 0,

    // a semantic edge is reflected in reachability analysis
    // keeping only semantic edges in the flow graph turns it into a dag
    Semantic = 1 << 1,

    // weak edges are also called up edges and point from the end of a loop switch option up to the loop switch
    Weak = Story,

    Strong = Story | Semantic,
}
