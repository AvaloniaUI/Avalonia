namespace Avalonia.Media
{
    public enum AcrylicBackgroundSource
    {
        HostBackDrop = 0,
        BackDrop = 1
    }

    public interface IPerlinNoiseBrush : IBrush
    {

    }

    public interface IBlurBrush : IBrush
    {

    }

    public interface IAcrylicBrush : IBrush
    {
        public  AcrylicBackgroundSource BackgroundSource { get; set; }

        public Color TintColor { get; set; }

        public double TintOpacity { get; set; }

        public Color FallbackColor { get; set; }
    }

    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public interface ISolidColorBrush : IBrush
    {
        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        Color Color { get; }
    }
}
