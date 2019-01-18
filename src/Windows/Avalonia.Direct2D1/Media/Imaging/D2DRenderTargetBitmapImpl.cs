// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Utilities;
using SharpDX;
using SharpDX.Direct2D1;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media.Imaging
{
    public class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IRenderTargetBitmapImpl, ILayerFactory
    {
        private readonly BitmapRenderTarget _renderTarget;

        public D2DRenderTargetBitmapImpl(BitmapRenderTarget renderTarget)
            : base(renderTarget.Bitmap)
        {
            _renderTarget = renderTarget;
        }

        public static D2DRenderTargetBitmapImpl CreateCompatible(
            SharpDX.Direct2D1.RenderTarget renderTarget,
            Size size)
        {
            var bitmapRenderTarget = new BitmapRenderTarget(
                renderTarget,
                CompatibleRenderTargetOptions.None,
                new Size2F((float)size.Width, (float)size.Height));
            return new D2DRenderTargetBitmapImpl(bitmapRenderTarget);
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new DrawingContextImpl(visualBrushRenderer, this, _renderTarget, null, () => Version++);
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            return CreateCompatible(_renderTarget, size);
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();
        }

        public override OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<D2DBitmap>(_renderTarget.Bitmap, false);
        }

        public override void Save(Stream stream)
        {
            using (var wic = new WicRenderTargetBitmapImpl(PixelSize, Dpi))
            {
                using (var dc = wic.CreateDrawingContext(null))
                {
                    dc.DrawImage(
                        RefCountable.CreateUnownedNotClonable(this),
                        1,
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)),
                        new Rect(PixelSize.ToSizeWithDpi(Dpi.X)));
                }

                wic.Save(stream);
            }
        }
    }
}
