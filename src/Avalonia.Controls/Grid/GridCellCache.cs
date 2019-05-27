// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Avalonia.Controls
{
    /// <summary>
    /// CellCache stored calculated values of
    /// 1. attached cell positioning properties;
    /// 2. size type;
    /// 3. index of a next cell in the group;
    /// </summary>
    internal struct GridCellCache
    {
        internal int ColumnIndex;
        internal int RowIndex;
        internal int ColumnSpan;
        internal int RowSpan;
        internal LayoutTimeSizeType SizeTypeU;
        internal LayoutTimeSizeType SizeTypeV;
        internal int Next;
        internal bool IsStarU { get { return ((SizeTypeU & LayoutTimeSizeType.Star) != 0); } }
        internal bool IsAutoU { get { return ((SizeTypeU & LayoutTimeSizeType.Auto) != 0); } }
        internal bool IsStarV { get { return ((SizeTypeV & LayoutTimeSizeType.Star) != 0); } }
        internal bool IsAutoV { get { return ((SizeTypeV & LayoutTimeSizeType.Auto) != 0); } }
    }
}