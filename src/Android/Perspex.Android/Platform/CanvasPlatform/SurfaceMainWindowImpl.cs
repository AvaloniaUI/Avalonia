using Android.Views;
using Perspex.Android.Platform.Specific;
using Perspex.Input;
using Perspex.Platform;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public class SurfaceMainWindowImpl : SurfaceWindowImpl, IWindowImpl
    {
        private IPointUnitService _pointService = PointUnitService.Instance;

        protected override void Init()
        {
            base.Init();

            HandleEvents = true;
            _keyboardHelper.ActivateAutoShowKeybord();
        }

        void ITopLevelImpl.Show()
        {
            (Parent as ViewGroup)?.RemoveAllViews();
            PerspexLocator.Current.GetService<IAndroidActivity>().ContentView = this;
            Visibility = ViewStates.Visible;
        }

        void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot)
        {
            base.SetInputRoot(inputRoot);
            _keyboardHelper.UpdateKeyboardState(inputRoot);
        }
    }
}