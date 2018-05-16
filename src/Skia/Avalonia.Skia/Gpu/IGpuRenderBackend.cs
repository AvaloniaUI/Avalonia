// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

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
        /// <param name="surfaces">Surfaces that will be used by this context.</param>
        /// <returns>Created Gpu render context.</returns>
        IGpuRenderContext CreateRenderContext(IEnumerable<object> surfaces);

        /// <summary>
        /// Create offscreen render context.
        /// </summary>
        /// <returns>Created Gpu render context.</returns>
        IGpuRenderContextBase CreateOffscreenRenderContext();
    }
}