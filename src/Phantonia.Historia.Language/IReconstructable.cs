using System.IO;

namespace Phantonia.Historia.Language;

public interface IReconstructable
{
    void Reconstruct(TextWriter writer);
}
