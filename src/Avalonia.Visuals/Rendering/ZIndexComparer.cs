using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class ZIndexComparer : IComparer<IVisual>
    {
        public static readonly ZIndexComparer Instance = new ZIndexComparer();

        public int Compare(IVisual x, IVisual y) => x.ZIndex.CompareTo(y.ZIndex);
    }
}
