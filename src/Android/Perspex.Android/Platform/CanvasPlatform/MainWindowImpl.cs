using Android.Views;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Input;
using Perspex.Platform;
using AG = Android.Graphics;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public class MainWindowImpl :
        //SurfaceWindowImpl
        WindowImpl
        , IWindowImpl
    {
        private IPointUnitService _pointService = PointUnitService.Instance;

        protected override void Init()
        {
            base.Init();

            HandleEvents = true;
            _keyboardHelper.ActivateAutoShowKeybord();
        }

        private int _statusbarHeight = -1;

        private int StatusBarHeight
        {
            get
            {
                if (_statusbarHeight < 0)
                {
                    var a = PerspexLocator.Current.GetService<IAndroidActivity>().Activity;
                    AG.Rect rectangle = new AG.Rect();
                    Window window = a.Window;
                    window.DecorView.GetWindowVisibleDisplayFrame(rectangle);
                    int statusBarHeight = rectangle.Top;
                    //int contentViewTop =
                    //    window.FindViewById(Window.IdAndroidContent).Top;
                    //int titleBarHeight = contentViewTop - statusBarHeight;
                    _statusbarHeight = statusBarHeight;
                }
                return _statusbarHeight;
            }
        }

        public override Point GetPerspexPointFromEvent(MotionEvent e)
        {
            //toolbar is on top of android screen and it's height is 50 ???
            //may be dependent from dpi ???
            //return base.GetPerspexPointFromEvent(e);
            //return new Point(e.GetX(0), e.GetY(0) + StatusBarHeight);

            var point = new Point(e.GetX(0), e.GetY(0) + (DrawType == ViewDrawType.CanvasOnDraw ? StatusBarHeight : 0));
            return _pointService.NativeToPerspex(point);
        }

        void ITopLevelImpl.Show()
        {
            (Parent as ViewGroup)?.RemoveAllViews();
            PerspexLocator.Current.GetService<IAndroidActivity>().ContentView = this;
            this.Visibility = ViewStates.Visible;
        }

        void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot)
        {
            base.SetInputRoot(inputRoot);
            _keyboardHelper.UpdateKeyboardState(inputRoot);
        }
    }
}