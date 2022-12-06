﻿using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class ZIndexComparer : IComparer<Visual>
    {
        public static readonly ZIndexComparer Instance = new ZIndexComparer();
        public static readonly Comparison<Visual> ComparisonInstance = Instance.Compare;

        public int Compare(Visual? x, Visual? y) => (x?.ZIndex ?? 0).CompareTo(y?.ZIndex ?? 0);
    }
}
