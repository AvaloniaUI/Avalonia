using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Rendering.SceneGraph.Media
{
    internal class SceneImageBrush : IImageBrush
    {
        public SceneImageBrush(IImageBrush source)
        {
            AlignmentX = source.AlignmentX;
            AlignmentY = source.AlignmentY;
            DestinationRect = source.DestinationRect;
            Opacity = source.Opacity;
            Source = source.Source;
            SourceRect = source.SourceRect;
            Stretch = source.Stretch;
            TileMode = source.TileMode;
        }

        public AlignmentX AlignmentX { get; }

        public AlignmentY AlignmentY { get; }

        public RelativeRect DestinationRect { get; }

        public double Opacity { get; }

        public IBitmap Source { get; }

        public RelativeRect SourceRect { get; }

        public Stretch Stretch { get; }

        public TileMode TileMode { get; }
    }
}
