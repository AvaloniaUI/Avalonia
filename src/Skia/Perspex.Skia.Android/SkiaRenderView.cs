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
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia.Android
{
    public abstract class SkiaRenderView : SkiaView
    {
        private IRenderTarget _renderTarget;

        public SkiaRenderView(Activity context) : base(context)
        {
            _renderTarget =
                PerspexLocator.Current.GetService<IPlatformRenderInterface>()
                    .CreateRenderer(this);
        }

        protected override void Draw()
        {
            if (_renderTarget == null)
                return;
            using (var ctx = _renderTarget.CreateDrawingContext())
                OnRender(ctx);
        }

        protected abstract void OnRender(DrawingContext ctx);

    }
}