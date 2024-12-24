﻿using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundRecordCreationExpressionNode() : ExpressionNode
{
    public required RecordCreationExpressionNode Original { get; init; }

    public required ImmutableArray<BoundArgumentNode> BoundArguments { get; init; }

    public required RecordTypeSymbol Record { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Original, .. BoundArguments];

    internal override void ReconstructCore(TextWriter writer)
    {
        Original.ReconstructCore(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{Original.GetDebuggerDisplay()} bound @ {Record.GetDebuggerDisplay()}";
}
