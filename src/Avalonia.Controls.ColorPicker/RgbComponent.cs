using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a specific component in the RGB color model.
    /// </summary>
    public enum RgbComponent
    {
        /// <summary>
        /// The Alpha component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="Color.A"/>
        /// </remarks>
        Alpha = 0,

        /// <summary>
        /// The Red component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="Color.R"/>
        /// </remarks>
        Red = 1,

        /// <summary>
        /// The Green component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="Color.G"/>
        /// </remarks>
        Green = 2,

        /// <summary>
        /// The Blue component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="Color.B"/>
        /// </remarks>
        Blue = 3
    };
}
