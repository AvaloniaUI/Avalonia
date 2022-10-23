using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform;

[Unstable]
public interface ITopLevelImplWithShareProvider : ITopLevelImpl
{
    public IShareProvider ShareProvider { get; }
}
