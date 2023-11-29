using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IWindowIconImpl
    {
        void Save(Stream outputStream);
    }

    [Unstable]
    public interface IThemeVariantWindowIconImpl : IWindowIconImpl
    {
        IWindowIconImpl Light { get; }
        IWindowIconImpl Dark { get; }
    }
}
