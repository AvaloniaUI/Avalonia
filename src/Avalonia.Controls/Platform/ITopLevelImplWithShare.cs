using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform;

[Unstable]
public interface ITopLevelImplWithShare : ITopLevelImpl
{
    public IShare Share { get; }
}
