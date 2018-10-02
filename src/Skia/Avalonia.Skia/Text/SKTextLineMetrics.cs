// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia
{   
    public struct SKTextLineMetrics
    {
        public SKTextLineMetrics(float width, float ascent, float descent, float leading)
        {
            Ascent = ascent;
            Descent = descent;
            Leading = leading;
            Size = new SKSize(width, descent - ascent + leading);            
            BaselineOrigin = new SKPoint(0, -ascent);
        }

        public float Ascent { get; }

        public float Descent { get; }

        public float Leading { get; }

        public SKSize Size { get; }

        public SKPoint BaselineOrigin { get; }
    }
}
