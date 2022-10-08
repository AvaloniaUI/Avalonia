namespace Avalonia.Platform
{
    /// <summary>
    /// Represents a single display screen.
    /// </summary>
    public class Screen
    {
        /// <summary>
        /// Gets the pixel density of the screen.
        /// This is a scaling factor so multiply by 100 to get a percentage.
        /// </summary>
        /// <remarks>
        /// Both X and Y density are assumed uniform.
        /// </remarks>
        public double PixelDensity { get; }

        /// <summary>
        /// Gets the overall pixel-size of the screen.
        /// This generally is the raw pixel counts in both the X and Y direction.
        /// </summary>
        public PixelRect Bounds { get; }

        /// <summary>
        /// Gets the actual working-area pixel-size of the screen.
        /// This may be smaller to account for notches and other block-out areas.
        /// </summary>
        public PixelRect WorkingArea { get; }

        /// <summary>
        /// Gets a value indicating whether the screen is the primary one.
        /// </summary>
        public bool IsPrimary { get; }
        
        public Screen(double pixelDensity, PixelRect bounds, PixelRect workingArea, bool isPrimary)
        {
            this.PixelDensity = pixelDensity;
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.IsPrimary = isPrimary;
        } 
    }
}
