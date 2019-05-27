// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Avalonia.Controls
{
    /// <summary>
    /// LayoutTimeSizeType is used internally and reflects layout-time size type.
    /// </summary>
    [System.Flags]
    internal enum LayoutTimeSizeType : byte
    {
        None = 0x00,
        Pixel = 0x01,
        Auto = 0x02,
        Star = 0x04,
    }
}