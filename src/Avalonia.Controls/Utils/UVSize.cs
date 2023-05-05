// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using Avalonia.Layout;

namespace Avalonia.Controls
{
    internal struct UVSize
    {
        internal UVSize(Orientation orientation, double width, double height)
        {
            U = V = 0d;
            Orientation = orientation;
            Width = width;
            Height = height;
        }

        internal UVSize(Orientation orientation)
        {
            U = V = 0d;
            Orientation = orientation;
        }

        internal double U;
        internal double V;
        internal Orientation Orientation;
        internal bool IsNaN => double.IsNaN(U) || double.IsNaN(V);

        internal double Width
        {
            get { return Orientation == Orientation.Horizontal ? U : V; }
            set { if (Orientation == Orientation.Horizontal) U = value; else V = value; }
        }
        internal double Height
        {
            get { return Orientation == Orientation.Horizontal ? V : U; }
            set { if (Orientation == Orientation.Horizontal) V = value; else U = value; }
        }
    }
}
