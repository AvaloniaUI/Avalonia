namespace Avalonia.Media
{
    public interface IPerlinNoiseBrush : IBrush
    {

    }

    public interface IBlurBrush : IBrush
    {

    }

    public interface IAcrylicBrush : IBrush
    {
        public Color Tint { get; set; }
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
