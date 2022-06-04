using Avalonia.Input;

namespace Avalonia.VisualTree
{
    public interface IOverlayHost
    {
        IInputElement? GetTopmostLightDismissElement();
    }
}
