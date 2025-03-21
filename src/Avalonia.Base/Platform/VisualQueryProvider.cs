using System;
using System.Runtime.InteropServices;

namespace Avalonia.Platform
{
    internal class VisualQueryProvider
    {
        private readonly Visual _visual;

        public double Width { get; private set; } = double.PositiveInfinity;

        public double Height { get; private set; } = double.PositiveInfinity;

        public VisualQueryProvider(Visual visual)
        {
            _visual = visual;
        }

        public event EventHandler? WidthChanged;
        public event EventHandler? HeightChanged;

        public virtual void SetSize(double width, double height, Styling.ContainerSizing containerType)
        {
            var currentWidth = Width;
            var currentHeight = Height;

            Width = width;
            Height = height;

            if (currentWidth != Width && (containerType == Styling.ContainerSizing.Width || containerType == Styling.ContainerSizing.WidthAndHeight))
                WidthChanged?.Invoke(this, EventArgs.Empty);
            if (currentHeight != Height && (containerType == Styling.ContainerSizing.Height || containerType == Styling.ContainerSizing.WidthAndHeight))
                HeightChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
