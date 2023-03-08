using Avalonia.Controls.Primitives.PopupPositioning;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific popup window implementation.
    /// </summary>
    public interface IPopupImpl : IWindowBaseImpl
    {
        IPopupPositioner? PopupPositioner { get; }

        void SetWindowManagerAddShadowHint(bool enabled);
    }
}
