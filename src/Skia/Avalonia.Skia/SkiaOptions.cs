// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Skia;

namespace Avalonia
{
    /// <summary>
    /// Options for Skia rendering subsystem.
    /// </summary>
    public class SkiaOptions
    {
        /// <summary>
        /// Custom gpu factory to use. Can be used to customize behavior of Skia renderer.
        /// </summary>
        public Func<ICustomSkiaGpu> CustomGpuFactory { get; set; }
    }
}
