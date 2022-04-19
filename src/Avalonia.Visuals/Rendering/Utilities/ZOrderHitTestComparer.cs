using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Utilities
{
    internal class ZOrderHitTestComparer : IComparer<(int, IVisual)>
    {
        public static readonly ZOrderHitTestComparer Instance = new();
        public int Compare((int, IVisual) x, (int, IVisual) y)
        {
            if (y.Item2 is null)
                return 1;

            var z = y.Item2.ZIndex - x.Item2.ZIndex;

            if (z != 0)
            {
                return z;
            }

            return y.Item1 - x.Item1;
        }
    }
}
