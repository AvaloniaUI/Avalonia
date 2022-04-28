namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a specific component within a color model.
    /// </summary>
    public enum ColorComponent
    {
        /// <summary>
        /// Represents the alpha component.
        /// </summary>
        Alpha = 0,

        /// <summary>
        /// Represents the first color component which is Red when RGB or Hue when HSV.
        /// </summary>
        Component1 = 1,

        /// <summary>
        /// Represents the second color component which is Green when RGB or Saturation when HSV.
        /// </summary>
        Component2 = 2,

        /// <summary>
        /// Represents the third color component which is Blue when RGB or Value when HSV.
        /// </summary>
        Component3 = 3
    }
}
