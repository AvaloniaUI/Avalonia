// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;
using System.IO;
using AG = Android.Graphics;

namespace Perspex.Android.CanvasRendering
{
    public class RenderTargetBitmapImpl : IRenderTargetBitmapImpl
    {
        private readonly RenderTarget _renderTarget;

        public RenderTargetBitmapImpl(AG.Bitmap surface)
        {
            Surface = surface;
            _renderTarget = new RenderTarget(Surface);
        }

        public int PixelWidth => Surface.Width;

        public int PixelHeight => Surface.Height;

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public AG.Bitmap Surface { get; }

        public void Save(string fileName)
        {
            using (var f = File.OpenWrite(fileName))
            {
                Surface.Compress(AG.Bitmap.CompressFormat.Png, 100, f);
            }
        }

        public Perspex.Media.DrawingContext CreateDrawingContext()
        {
            return _renderTarget.CreateDrawingContext();
        }

        //void IRenderTarget.Resize(int width, int height)
        //{
        //    this._renderTarget.Resize(width, height);
        //}
    }
}