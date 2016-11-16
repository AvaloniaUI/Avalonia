// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a render target
    /// </summary>
    /// <remarks>
    /// The interface used for obtaining drawing context from surfaces you can render on.
    /// </remarks>
    public interface IRenderTarget : IDisposable
    {
        /// <summary>
        /// Creates an <see cref="DrawingContext"/> for a rendering session.
        /// </summary>
        DrawingContext CreateDrawingContext();
    }
}
