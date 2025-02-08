using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language;

public sealed class LineIndexing(ImmutableDictionary<string, ImmutableArray<long>> pathLines)
{
    private readonly ImmutableList<(long index, string path)> indexPaths =
        pathLines.Select(kvp => (index: kvp.Value[0], path: kvp.Key))
                 .OrderBy(t => t.index)
                 .ToImmutableList();

    // line and character are both 1-based
    public long GetIndex(LineCharacter lineCharacter) => pathLines[lineCharacter.Path][lineCharacter.Line - 1] + lineCharacter.Character - 1;

    public LineCharacter GetLineCharacter(long index)
    {
        int indexPathsIndex = BinarySearchIndexPaths(index);
        (_, string path) = indexPaths[indexPathsIndex];

        int lineIndex = ImmutableArray.BinarySearch(pathLines[path], index);

        if (lineIndex < 0)
        {
            // this means the index was not found exactly, so the greatest index less than it is the 2's complement
            lineIndex = ~lineIndex;
            lineIndex--;
        }

        int character = (int)(index - pathLines[path][lineIndex]);
        
        // line and character are both 1-based
        return new LineCharacter(lineIndex + 1, character + 1, path);
    }

    private int BinarySearchIndexPaths(long index)
    {
        // adapted from .NET source code: Array.BinarySearch
        int lo = 0;
        int hi = indexPaths.Count - 1;

        while (lo <= hi)
        {
            int i = lo + ((hi - lo) >> 1);

            if (indexPaths[i].index == index)
            {
                return i;
            }

            if (indexPaths[i].index < index)
            {
                lo = i + 1;
            }
            else
            {
                hi = i - 1;
            }
        }

        // we want the index where the value is the greatest value less than 'index'
        return lo - 1;
    }
}
