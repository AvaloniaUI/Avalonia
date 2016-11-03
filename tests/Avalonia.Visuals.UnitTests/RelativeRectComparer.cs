// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Visuals.UnitTests
{
    public class RelativeRectComparer : IEqualityComparer<RelativeRect>
    {
        public bool Equals(RelativeRect a, RelativeRect b)
        {
            return a.Unit == b.Unit &&
                   Math.Round(a.Rect.X, 3) == Math.Round(b.Rect.X, 3) &&
                   Math.Round(a.Rect.Y, 3) == Math.Round(b.Rect.Y, 3) &&
                   Math.Round(a.Rect.Width, 3) == Math.Round(b.Rect.Width, 3) &&
                   Math.Round(a.Rect.Height, 3) == Math.Round(b.Rect.Height, 3);
        }

        public int GetHashCode(RelativeRect obj)
        {
            throw new NotImplementedException();
        }
    }
}
