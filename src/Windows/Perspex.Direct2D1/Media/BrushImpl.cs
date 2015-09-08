// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Direct2D1.Media
{
    public abstract class BrushImpl : IDisposable
    {
        public SharpDX.Direct2D1.Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (this.PlatformBrush != null)
            {
                this.PlatformBrush.Dispose();
            }
        }
    }
}
