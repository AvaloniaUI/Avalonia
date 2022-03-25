using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Utilities
{
    internal class ZOrderHitTestComparer : IComparer<KeyValuePair<int, IVisual>>
    {
        public static readonly ZOrderHitTestComparer Instance = new ZOrderHitTestComparer();
        public int Compare(KeyValuePair<int, IVisual> x, KeyValuePair<int, IVisual> y)
        {
            if (y.Value is null)
                return 1;

            var z = y.Value.ZIndex - x.Value.ZIndex;

            if (z != 0)
            {
                return z;
            }

            return y.Key - x.Key;
        }
    }
}
