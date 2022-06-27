namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a specific tab/page (subview) within the <see cref="ColorView"/>.
    /// </summary>
    /// <remarks>
    /// This is indexed to match the default control template ordering.
    /// </remarks>
    public enum ColorViewTab
    {
        /// <summary>
        /// The color spectrum subview with a box/ring spectrum and sliders.
        /// </summary>
        Spectrum = 0,

        /// <summary>
        /// The color palette subview with a grid of selectable colors.
        /// </summary>
        Palette = 1,

        /// <summary>
        /// The components subview with sliders and numeric input boxes.
        /// </summary>
        Components = 2,
    }
}
