// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;
using SharpDX;

namespace Perspex.Direct2D1.Media
{
    internal class BrushWrapper : ComObject
    {
        public BrushWrapper(Brush brush, FontWeight weight, double fontSize)
        {
            Brush = brush;
            FontWeight = weight;
            FontSize = fontSize;
        }

        public Brush Brush { get; private set; }
        public FontWeight FontWeight { get; private set; }
        public double FontSize { get; private set; }
    }
}
