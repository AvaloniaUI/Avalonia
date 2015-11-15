// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Android.Content;
using Android.Views;
using Android.Widget;
using Perspex.Android.Platform.Specific;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;
using System;
using AG = Android.Graphics;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public class PopupImpl : PopupWindow, IPopupImpl, IPlatformHandle
    {
        private class PopupPerspexViewContent : WindowImpl
        {
            private Func<MotionEvent, bool?> _funcDispatchTouchEvent;

            public PopupPerspexViewContent(Context context, Func<MotionEvent, bool?> funcDispatchTouchEvent) : base(context)
            {
                this._funcDispatchTouchEvent = funcDispatchTouchEvent;
            }

            public override bool DispatchTouchEvent(MotionEvent e)
            {
                bool? res = _funcDispatchTouchEvent(e);

                return res == null ? base.DispatchTouchEvent(e) : res.Value;
            }
        }

        private PopupPerspexViewContent content;

        private View mainView;

        //public PopupImpl(Context context) : base(context)
        public PopupImpl()
        {
            ContentView = content = new PopupPerspexViewContent(PerspexLocator.Current.GetService<IAndroidActivity>().Activity, DispatchTouchEvent);
            ContentView.Background = new AG.Drawables.ColorDrawable(AG.Color.Transparent);
            mainView = PerspexLocator.Current.GetService<IAndroidActivity>().ContentView.View;
            //Background = new AG.Drawables.ColorDrawable(AG.Color.);
        }

        //public PopupImpl() : this(PerspexLocator.Current.GetService<IAndroidActivity>().Activity)
        //{
        //}

        private bool? DispatchTouchEvent(MotionEvent e)
        {
            var p = content.GetPerspexPointFromEvent(e);

            Rect clientBounds = new Rect(ClientSize);

            if (clientBounds.Contains(p))
            {
                return null;
            }

            return mainView.DispatchTouchEvent(e);
        }

        public Size MaxClientSize => content.MaxClientSize;

        public Action Activated { get { return (content as ITopLevelImpl).Activated; } set { (content as ITopLevelImpl).Activated = value; } }

        public Size ClientSize { get { return content.ClientSize; } set { content.ClientSize = value; } }

        public Action Closed { get { return content.Closed; } set { content.Closed = value; } }

        public Action Deactivated { get { return content.Deactivated; } set { content.Deactivated = value; } }

        public Action<RawInputEventArgs> Input { get { return content.Input; } set { content.Input = value; } }

        public Action<Rect> Paint { get { return content.Paint; } set { content.Paint = value; } }

        public Action<Size> Resized { get { return content.Resized; } set { content.Resized = value; } }

        IntPtr IPlatformHandle.Handle => content.Handle;

        IPlatformHandle ITopLevelImpl.Handle => content;

        public string HandleDescriptor => "PopupImpl";

        public void Activate() => content.Activate();

        public void Invalidate(Rect rect) => content.Invalidate(rect);

        public Point PointToScreen(Point point)
        {
            return content.PointToScreen(point);
        }

        public void SetCursor(IPlatformHandle cursor)
        {
            content.SetCursor(cursor);
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            content.SetInputRoot(inputRoot);
        }

        private Point position;

        public void SetPosition(Point p)
        {
            position = p;
        }

        public void Show()
        {
            content.HandleEvents = true;
            var ps = PointUnitService.Instance;
            //content.Activate();
            Width = ps.PerspexToNativeXInt(MaxClientSize.Width - position.X);
            Height = ps.PerspexToNativeYInt(MaxClientSize.Height - position.Y);
            content.LayoutParameters = new ViewGroup.LayoutParams(Width, Height);
            // content.ClientSize = new Size(MaxClientSize.Width - position.X, MaxClientSize.Height - position.Y);
            //Width = Convert.ToInt32(ClientSize.Width);
            //Height = Convert.ToInt32(ClientSize.Height);

            base.ShowAtLocation(mainView, GravityFlags.Top | GravityFlags.Left, ps.PerspexToNativeXInt(position.X), ps.PerspexToNativeYInt(position.Y));
            content.Invalidate();
        }

        public void Hide()
        {
            content.HandleEvents = false;
            //content.Hide();
            base.Dismiss();
        }
    }
}