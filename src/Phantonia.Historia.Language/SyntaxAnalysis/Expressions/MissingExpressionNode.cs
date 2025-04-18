﻿using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record MissingExpressionNode() : ExpressionNode
{
    public override IEnumerable<SyntaxNode> Children => [];

    public override bool IsConstant => false;

    protected override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => "<missing expression>";
}
