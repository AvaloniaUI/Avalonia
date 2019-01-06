// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia platform render interface.
    /// </summary>
    public class PlatformRenderInterface : IPlatformRenderInterface
    {
        private GRContext GrContext { get; }

        public PlatformRenderInterface()
        {
            var gl = AvaloniaLocator.Current.GetService<IWindowingPlatformGlFeature>();
            if (gl != null)
            {
                var display = gl.ImmediateContext.Display;
                var iface = display.Type == GlDisplayType.OpenGL2
                    ? GRGlInterface.AssembleGlInterface((_, proc) => display.GlInterface.GetProcAddress(proc))
                    : GRGlInterface.AssembleGlesInterface((_, proc) => display.GlInterface.GetProcAddress(proc));
                gl.ImmediateContext.MakeCurrent();
                GrContext = GRContext.Create(GRBackend.OpenGL, iface);
            }
        }

        /// <inheritdoc />
        public IFormattedTextImpl CreateFormattedText(
            string text,
            Typeface typeface,
            double fontSize,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            return new FormattedTextImpl(text, typeface, fontSize, textAlignment, wrapping, constraint, spans);
        }

        /// <inheritdoc />
        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new ImmutableBitmap(stream);
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return LoadBitmap(stream);
            }
        }

        /// <inheritdoc />
        public IBitmapImpl LoadBitmap(PixelFormat format, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            return new ImmutableBitmap(size, dpi, stride, format, data);
        }

        /// <inheritdoc />
        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(PixelSize size, Vector dpi)
        {
            if (size.Width < 1)
            {
                throw new ArgumentException("Width can't be less than 1", nameof(size));
            }

            if (size.Height < 1)
            {
                throw new ArgumentException("Height can't be less than 1", nameof(size));
            }

            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = size.Width,
                Height = size.Height,
                Dpi = dpi,
                DisableTextLcdRendering = false,
                GrContext = GrContext
            };

            return new SurfaceRenderTarget(createInfo);
        }

        /// <inheritdoc />
        public virtual IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IGlPlatformSurface glSurface && GrContext != null)
                {
                    return new GlRenderTarget(GrContext, glSurface);
                }
                if (surface is IFramebufferPlatformSurface framebufferSurface)
                {
                    return new FramebufferRenderTarget(framebufferSurface);
                }
            }

            throw new NotSupportedException(
                "Don't know how to create a Skia render target from any of provided surfaces");
        }

        /// <inheritdoc />
        public IWriteableBitmapImpl CreateWriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null)
        {
            return new WriteableBitmapImpl(size, dpi, format);
        }
    }
}
