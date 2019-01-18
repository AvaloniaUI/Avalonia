using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class PopupImpl : TopLevelImpl, IPopupImpl
    {
        private PixelPoint _position;
        private bool _isAdded;
        Action IWindowBaseImpl.Activated { get; set; }
        public Action<PixelPoint> PositionChanged { get; set; }
        public Action Deactivated { get; set; }

        public PopupImpl() : base(ActivityTracker.Current, true)
        {
        }

        private Size _clientSize = new Size(1, 1);

        public void Resize(Size value)
        {
            if (View == null)
                return;
            _clientSize = value;
            UpdateParams();
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public IScreenImpl Screen { get; }

        public PixelPoint Position
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


        public void Activate()
        {
        }

        public void BeginMoveDrag()
        {
            //Not supported
        }

        public void BeginResizeDrag(WindowEdge edge)
        {
            //Not supported
        }

        public void SetTopmost(bool value)
        {
            //Not supported
        }
    }
}
