using System;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph.Media
{
    internal class SceneVisualBrush : IVisualBrush
    {
        public SceneVisualBrush(IVisualBrush source)
        {
            AlignmentX = source.AlignmentX;
            AlignmentY = source.AlignmentY;
            DestinationRect = source.DestinationRect;
            Opacity = source.Opacity;
            SourceRect = source.SourceRect;
            Stretch = source.Stretch;
            TileMode = source.TileMode;
            Visual = source.Visual;
        }

        public AlignmentX AlignmentX { get; }

        public AlignmentY AlignmentY { get; }

        public RelativeRect DestinationRect { get; }

        public double Opacity { get; }

        public RelativeRect SourceRect { get; }

        public Stretch Stretch { get; }

        public TileMode TileMode { get; }

        public IVisual Visual { get; }
    }
}
