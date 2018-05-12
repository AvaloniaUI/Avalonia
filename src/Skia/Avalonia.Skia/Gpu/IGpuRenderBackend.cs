// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// Manage Gpu render context access and creation.
    /// </summary>
    public interface IGpuRenderBackend
    {
        /// <summary>
        /// Create render context for given platform handle.
        /// </summary>
        /// <param name="platformHandle">Platform handle to use.</param>
        /// <returns></returns>
        IGpuRenderContext CreateRenderContext(IPlatformHandle platformHandle);

        /// <summary>
        /// Resource render context for offscreen rendering.
        /// </summary>
        IGpuRenderContext ResourceRenderContext { get; }
    }
}