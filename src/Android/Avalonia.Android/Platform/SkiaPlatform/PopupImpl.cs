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
using Avalonia.Platform;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class PopupImpl : TopLevelImpl, IPopupImpl
    {
        private Point _position;
        private bool _isAdded;
        public PopupImpl() : base(ActivityTracker.Current, true)
        {
        }

        private Size _clientSize = new Size(1, 1);
        public override Size ClientSize
        {
            get { return base.ClientSize; }
            set
            {
                if(View == null)
                    return;
                _clientSize = value;
                UpdateParams();
            }
        }

        public override Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
                PositionChanged?.Invoke(_position);
                UpdateParams();
            }
        }

        WindowManagerLayoutParams CreateParams() => new WindowManagerLayoutParams(0,
            WindowManagerFlags.NotTouchModal, Format.Translucent)
        {
            Gravity = GravityFlags.Left | GravityFlags.Top,
            WindowAnimations = 0,
            X = (int) _position.X,
            Y = (int) _position.Y,
            Width = Math.Max(1, (int) _clientSize.Width),
            Height = Math.Max(1, (int) _clientSize.Height)
        };

        void UpdateParams()
        {
            if (_isAdded)
                ActivityTracker.Current?.WindowManager?.UpdateViewLayout(View, CreateParams());
        }

        public override void Show()
        {
            if (_isAdded)
                return;
            ActivityTracker.Current.WindowManager.AddView(View, CreateParams());
            _isAdded = true;
        }

        public override void Hide()
        {
            if (_isAdded)
            {
                var wm = View.Context.ApplicationContext.GetSystemService(Context.WindowService)
                    .JavaCast<IWindowManager>();
                wm.RemoveView(View);
                _isAdded = false;
            }
        }

        public override void Dispose()
        {
            Hide();
            base.Dispose();
        }
    }
}