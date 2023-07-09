using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;
using System;

namespace Phantonia.Historia.Language;

// boobies begone
public sealed class Binder
{
    public Binder(StoryNode story)
    {
        this.story = story;
    }

    private readonly StoryNode story;

    public event Action<Error>? ErrorFound;

    public StoryNode Bind()
    {
        // this will get significantly more complicated once we actually get symbols to bind to...

        // right now we do not allow any scenes beside the main scene, but we require a main scene
        int mainCount = 0;
        int? secondMainIndex = null;

        foreach (SymbolDeclarationNode symbolDeclaration in story.Symbols)
        {
            if (symbolDeclaration is SceneSymbolDeclarationNode { Name: "main" })
            {
                mainCount++;

                if (mainCount == 2)
                {
                    secondMainIndex = symbolDeclaration.Index;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        if (mainCount != 1)
        {
            ErrorFound?.Invoke(new Error { ErrorMessage = $"A story needs exactly one main scene (has {mainCount})", Index = secondMainIndex ?? 0 });
        }

        return story;
    }
}
