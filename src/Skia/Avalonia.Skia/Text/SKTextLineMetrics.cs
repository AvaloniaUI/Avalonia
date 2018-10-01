// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia
{   
    public class SKTextLineMetrics
    {
        public SKTextLineMetrics(float width, float height, SKPoint baselineOrigin)
        {
            Size = new SKSize(width, height);
            BaselineOrigin = baselineOrigin;
        }

        public SKSize Size { get; }

        public SKPoint BaselineOrigin { get; }
    }
}
