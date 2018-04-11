namespace Avalonia.Animation
{
    /// <summary>
    /// Defines the intermediate color space
    /// in which the color interpolation will be done.
    /// </summary>
    public enum ColorInterpolationMode
    {
        /// <summary>
        /// Premultiply the alpha component and interpolate from RGB color space.
        /// Fast but relatively accurate, perception-wise.
        /// </summary>
        PremultipliedRGB,

        /// <summary>
        /// Directly interpolate from RGB color space. 
        /// Fastest but the least accurate, perception-wise.
        /// </summary>
        RGB,

        /// <summary>
        /// Converts RGB into HSV color space and interpolates. 
        /// Slow but accurate, perception-wise.
        /// </summary>
        HSV,

        /// <summary>
        /// Converts RGB into HSV color space and interpolates. 
        /// Slowest but the most accurate, perception-wise.
        /// </summary>
        LAB,

    }
}