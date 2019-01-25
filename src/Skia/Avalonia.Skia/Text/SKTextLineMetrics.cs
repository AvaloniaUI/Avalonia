// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia.Text
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

        /// <summary>
        /// Gets the ascent.
        /// </summary>
        /// <value>
        /// The ascent.
        /// </value>
        public float Ascent { get; }

        /// <summary>
        /// Gets the descent.
        /// </summary>
        /// <value>
        /// The descent.
        /// </value>
        public float Descent { get; }

        /// <summary>
        /// Gets the leading.
        /// </summary>
        /// <value>
        /// The leading.
        /// </value>
        public float Leading { get; }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public SKSize Size { get; }

        /// <summary>
        /// Gets the baseline origin.
        /// </summary>
        /// <value>
        /// The baseline origin.
        /// </value>
        public SKPoint BaselineOrigin { get; }
    }
}
