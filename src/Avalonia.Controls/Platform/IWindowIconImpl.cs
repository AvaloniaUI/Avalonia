using System.IO;

namespace Avalonia.Platform
{
    public interface IWindowIconImpl
    {
        void Save(Stream outputStream);
    }
}
