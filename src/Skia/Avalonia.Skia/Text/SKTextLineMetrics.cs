// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Skia
{
    public class SKTextLineMetrics
    {
        public SKTextLineMetrics(float width, float height, Point baselineOrigin)
        {
            Size = new Size(width, height);
            BaselineOrigin = baselineOrigin;
        }

        public Size Size { get; }

        public Point BaselineOrigin { get; }
    }
}
