using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Takes the solid background of a top level, for the platform to fill areas no frame has
    /// been drawn for yet (for example the part of a resized window that a buffer of the
    /// previous size does not cover).
    /// </summary>
    [PrivateApi]
    public interface ITopLevelBackgroundHint
    {
        /// <summary>
        /// Sets the background color, or null when the background is not a single opaque color.
        /// </summary>
        void SetBackgroundHint(Color? color);
    }
}
