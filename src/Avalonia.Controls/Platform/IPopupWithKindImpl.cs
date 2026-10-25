namespace Avalonia.Platform
{
    /// <summary>
    /// Describes what a popup window is used for.
    /// </summary>
    internal enum PopupKind
    {
        /// <summary>
        /// A menu, flyout, drop-down or any other popup.
        /// </summary>
        Default,

        /// <summary>
        /// A tooltip.
        /// </summary>
        ToolTip
    }

    /// <summary>
    /// Implemented by popup windows that can tell the window manager what they are used for.
    /// </summary>
    internal interface IPopupWithKindImpl : IPopupImpl
    {
        /// <summary>
        /// Sets the kind of the popup. Called before the popup is shown.
        /// </summary>
        void SetPopupKind(PopupKind kind);
    }
}
