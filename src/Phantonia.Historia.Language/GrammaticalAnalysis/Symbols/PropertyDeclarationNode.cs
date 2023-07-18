﻿using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;

public record PropertyDeclarationNode : SyntaxNode
{
    public PropertyDeclarationNode() { }

    public required string Name { get; init; }

    public required TypeNode Type { get; init; }
}
