// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using SharpDX;
using SharpDX.Direct2D1;
using DrawingContext = Avalonia.Media.DrawingContext;
using DwFactory = SharpDX.DirectWrite.Factory;

namespace Avalonia.Direct2D1
{
    public class RenderTarget : IRenderTarget
    {
        /// <summary>
        /// The render target.
        /// </summary>
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderTarget"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        public RenderTarget(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            Direct2DFactory = AvaloniaLocator.Current.GetService<Factory>();
            DirectWriteFactory = AvaloniaLocator.Current.GetService<DwFactory>();
            _renderTarget = renderTarget;
        }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public Factory Direct2DFactory
        {
            get;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public DwFactory DirectWriteFactory
        {
            get;
        }

        /// <summary>
        /// Creates a drawing context for a rendering session.
        /// </summary>
        /// <returns>An <see cref="Avalonia.Media.DrawingContext"/>.</returns>
        public DrawingContext CreateDrawingContext()
        {
            return new DrawingContext(new Media.DrawingContext(_renderTarget, DirectWriteFactory));
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }
    }
}
