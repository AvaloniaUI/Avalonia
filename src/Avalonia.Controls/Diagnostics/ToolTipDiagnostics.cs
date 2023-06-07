namespace Avalonia.Controls.Diagnostics
{
    /// <summary>
    /// Helper class to provide diagnostics information for <see cref="ToolTip"/>.
    /// </summary>
    public static class ToolTipDiagnostics
    {
        /// <summary>
        /// Provides access to the internal <see cref="ToolTip.ToolTipProperty"/> for use in DevTools.
        /// </summary>
        public static readonly AvaloniaProperty<ToolTip?> ToolTipProperty = ToolTip.ToolTipProperty;
    }
}
