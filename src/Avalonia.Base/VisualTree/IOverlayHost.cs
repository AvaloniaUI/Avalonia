using Avalonia.Input;
using Avalonia.Metadata;

namespace Avalonia.VisualTree
{
    [Unstable]
    public interface IOverlayHost
    {
        IInputElement? GetTopmostLightDismissElement();
    }
}
