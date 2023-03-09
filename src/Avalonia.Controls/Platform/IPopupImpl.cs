using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific popup window implementation.
    /// </summary>
    [Unstable]
    public interface IPopupImpl : IWindowBaseImpl
    {
        IPopupPositioner? PopupPositioner { get; }

        void SetWindowManagerAddShadowHint(bool enabled);
    }
}
