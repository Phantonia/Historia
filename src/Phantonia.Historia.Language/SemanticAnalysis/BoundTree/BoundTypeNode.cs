﻿using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundTypeNode : TypeNode
{
    public BoundTypeNode() { }

    public required TypeNode Original { get; init; }

    public required TypeSymbol Symbol { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Original];

    internal override string ReconstructCore() => Original.ReconstructCore();

    internal override void ReconstructCore(TextWriter writer)
    {
        Original.ReconstructCore(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{Original.GetDebuggerDisplay()} bound @ {Symbol.GetDebuggerDisplay()}";
}
