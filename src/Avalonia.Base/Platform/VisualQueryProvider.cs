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


/* Unmerged change from project 'Avalonia.Base (netstandard2.0)'
Before:
        public virtual void SetSize(double width, double height, Layout.ContainerSizing containerType)
        {
After:
        public virtual void SetSize(double width, double height, ContainerSizing containerType)
        {
*/
        public virtual void SetSize(double width, double height, Styling.ContainerSizing containerType)
        {
            var currentWidth = Width;
            var currentHeight = Height;

            Width = width;
            Height = height;


/* Unmerged change from project 'Avalonia.Base (netstandard2.0)'
Before:
            if (currentWidth != Width && (containerType == Layout.ContainerSizing.Width || containerType == Layout.ContainerSizing.WidthAndHeight))
                WidthChanged?.Invoke(this, EventArgs.Empty);
            if (currentHeight != Height && (containerType == Layout.ContainerSizing.Height || containerType == Layout.ContainerSizing.WidthAndHeight))
                HeightChanged?.Invoke(this, EventArgs.Empty);
After:
            if (currentWidth != Width && (containerType == ContainerSizing.Width || containerType == ContainerSizing.WidthAndHeight))
                WidthChanged?.Invoke(this, EventArgs.Empty);
            if (currentHeight != Height && (containerType == ContainerSizing.Height || containerType == ContainerSizing.WidthAndHeight))
                HeightChanged?.Invoke(this, EventArgs.Empty);
*/
            if (currentWidth != Width && (containerType == Styling.ContainerSizing.Width || containerType == Styling.ContainerSizing.WidthAndHeight))
                WidthChanged?.Invoke(this, EventArgs.Empty);
            if (currentHeight != Height && (containerType == Styling.ContainerSizing.Height || containerType == Styling.ContainerSizing.WidthAndHeight))
                HeightChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
