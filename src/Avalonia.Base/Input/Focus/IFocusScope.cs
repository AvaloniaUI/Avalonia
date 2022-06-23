using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public interface IFocusScope
    {
        FocusManager FocusManager { get; }

        IOverlayHost? OverlayHost { get; }
    }
}
