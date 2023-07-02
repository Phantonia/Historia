using Phantonia.Historia.Language.Ast;

namespace Phantonia.Historia.Language;

// boobies begone
public sealed class Binder
{
    public Binder(StoryNode story)
    {
        this.story = story;
    }

    private readonly StoryNode story;

    public StoryNode Bind()
    {
        // this will get significantly more complicated once we actually get symbols to bind to...
        return story;
    }
}
