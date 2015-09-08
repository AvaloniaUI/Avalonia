// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;
using SharpDX;

namespace Perspex.Direct2D1.Media
{
    internal class BrushWrapper : ComObject
    {
        public BrushWrapper(Brush brush)
        {
            Brush = brush;
        }

        public Brush Brush { get; private set; }
    }
}
