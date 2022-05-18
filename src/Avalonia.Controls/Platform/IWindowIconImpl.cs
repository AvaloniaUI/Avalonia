using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IWindowIconImpl
    {
        void Save(Stream outputStream);
    }
}
