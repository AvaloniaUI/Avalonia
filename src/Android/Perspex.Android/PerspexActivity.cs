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
using Perspex.Android.Rendering;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;
using APoint = Android.Graphics.Point;

namespace Perspex.Android
{
    public class PerspexActivity : Activity, IWindowImpl
    {
        public IInputRoot InputRoot { get; set; }

        public PerspexView PerspexView { get; private set; }

        public PerspexActivity()
        {
            PerspexView = new PerspexView(this);
            Handle = new PlatformHandle(PerspexView.Handle.Handle, "Perspex Activity");
        }

        public new IPlatformHandle Handle { get; }
        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect> Paint { get; set; }
        public Action<Size> Resized { get; set; }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }

        public Size ClientSize
        {
            get
            {
                return new Size(Resources.DisplayMetrics.WidthPixels,
                    Resources.DisplayMetrics.HeightPixels);
            }
            set { }
        }

        public Size MaxClientSize
        {
            get
            {
                return new Size(Resources.DisplayMetrics.WidthPixels,
                    Resources.DisplayMetrics.HeightPixels);
            }
        }

        public void SetTitle(string title)
        {
            throw new NotImplementedException();
        }

        public void Show()
        {
            throw new NotImplementedException();
        }

        public IDisposable ShowDialog()
        {
            throw new NotImplementedException();
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Hide()
        {
            throw new NotImplementedException();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(PerspexView);
        }
    }
}