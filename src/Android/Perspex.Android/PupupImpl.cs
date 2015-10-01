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
using Perspex.Controls;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;

namespace Perspex.Android
{
    public class PopupImpl : IPopupImpl
    {
        public Action Activated
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Size ClientSize
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Action Closed
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Action Deactivated
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IPlatformHandle Handle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Action<RawInputEventArgs> Input
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Action<Rect> Paint
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Action<Size> Resized
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
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

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public void SetInputRoot(IInputRoot inputRoot)
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

        public void SetOwner(TopLevel owner)
        {
            throw new NotImplementedException();
        }

        public void SetPosition(Point p)
        {
            throw new NotImplementedException();
        }

        public void Show()
        {
            throw new NotImplementedException();
        }
    }
}