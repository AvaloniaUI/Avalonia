// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Direct2D1.UnitTests
{
    public class RectComparer : IEqualityComparer<Rect>
    {
        public bool Equals(Rect a, Rect b)
        {
            return Math.Round(a.X, 3) == Math.Round(b.X, 3) &&
                   Math.Round(a.Y, 3) == Math.Round(b.Y, 3) &&
                   Math.Round(a.Width, 3) == Math.Round(b.Width, 3) &&
                   Math.Round(a.Height, 3) == Math.Round(b.Height, 3);
        }

        public int GetHashCode(Rect obj)
        {
            throw new NotImplementedException();
        }
    }
}
