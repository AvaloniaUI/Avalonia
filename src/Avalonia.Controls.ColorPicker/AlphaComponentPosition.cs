namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the position of a color's alpha component relative to all other components.
    /// </summary>
    public enum AlphaComponentPosition
    {
        /// <summary>
        /// The alpha component occurs before all other components.
        /// </summary>
        /// <remarks>
        /// For example, this may indicate the #AARRGGBB or ARGB format which
        /// is the default format for XAML itself and the Color struct.
        /// </remarks>
        Leading,

        /// <summary>
        /// The alpha component occurs after all other components.
        /// </summary>
        /// <remarks>
        /// For example, this may indicate the #RRGGBBAA or RGBA format which
        /// is the default format for CSS.
        /// </remarks>
        Trailing,
    }
}
