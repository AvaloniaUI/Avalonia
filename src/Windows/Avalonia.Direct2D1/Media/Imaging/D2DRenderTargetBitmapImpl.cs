// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using Avalonia.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media.Imaging
{
    public class D2DRenderTargetBitmapImpl : D2DBitmapImpl, IRenderTargetBitmapImpl, ILayerFactory
    {
        private readonly BitmapRenderTarget _target;

        public D2DRenderTargetBitmapImpl(BitmapRenderTarget target)
            : base(target.Bitmap)
        {
            _target = target;
        }

        public override int PixelWidth => _target.PixelSize.Width;
        public override int PixelHeight => _target.PixelSize.Height;

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
            return new DrawingContextImpl(
                visualBrushRenderer,
                this,
                _target.QueryInterface<DeviceContext>());
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            return CreateCompatible(_target, size);
        }

        public override void Dispose()
        {
            _target.Dispose();
        }

        public override OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<D2DBitmap>(_target.Bitmap, false);
        }
    }
}
