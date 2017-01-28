using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Skia.Android
{
    public abstract class SkiaRenderView : SkiaView
    {
        private IRenderTarget _renderTarget;

        public SkiaRenderView(Activity context) : base(context)
        {
            _renderTarget =
                AvaloniaLocator.Current.GetService<IPlatformRenderInterface>()
                    .CreateRenderTarget(this);
        }

        protected override void Draw()
        {
            if (_renderTarget == null)
                return;
            using (var ctx = _renderTarget.CreateDrawingContext(null))
                OnRender(new DrawingContext(ctx));
        }

        protected abstract void OnRender(DrawingContext ctx);

    }
}