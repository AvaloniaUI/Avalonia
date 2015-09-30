using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Javax.Security.Auth;
using Perspex.Controls;
using Perspex.Input.Raw;
using Perspex.Platform;

namespace Perspex.Android
{
    public class WindowImpl : IWindowImpl
    {
        public TopLevel Owner { get; private set; }

        private Activity _activity;

        public WindowImpl()
        {
            _activity = new Activity();
            Handle = new PlatformHandle(_activity.Handle, "Activity");
        }

        public Size ClientSize
        {
            get
            {
                
                return new Size(_activity.Resources.DisplayMetrics.WidthPixels, _activity.Resources.DisplayMetrics.HeightPixels);
            }
            set
            {
                
            }
        }

        public IPlatformHandle Handle
        {
            get;
            private set;
        }

        public Action Created { get; set; }
        public Action Activated { get; set; }
        public Action Closed { get; set; }
        public Action Deactivated { get; set; }
        public Action<RawInputEventArgs> Input { get; set; }
        public Action<Rect, IPlatformHandle> Paint { get; set; }
        public Action<Size> Resized { get; set; }
        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public void SetOwner(TopLevel owner)
        {
            throw new NotImplementedException();
        }

        public Point PointToScreen(Point point)
        {
            throw new NotImplementedException();
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Size MaxClientSize { get; }
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

        public void Hide()
        {
            throw new NotImplementedException();
        }
    }
}