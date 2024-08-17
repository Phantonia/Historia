using System;

namespace Phantonia.Historia.Language.FlowAnalysis;

[Flags]
public enum FlowVertexKind
{
    Visible,
    Invisible,
    PurelySemantic,
}
