using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Android.Rendering
{
    public class PerspexCanvas : Canvas, IRenderTarget
    {
        public PerspexCanvas(IPlatformHandle handle, double width, double height)
        {
        }

        public void Dispose()
        {

        }

        public IDrawingContext CreateDrawingContext()
        {
            return new DrawingContext();
        }

        public void Resize(int width, int height)
        {

        }
    }
}