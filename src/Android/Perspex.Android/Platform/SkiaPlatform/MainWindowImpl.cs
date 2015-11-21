using Android.Views;
using Perspex.Android.Platform.Specific;
using Perspex.Input;
using Perspex.Platform;

namespace Perspex.Android.Platform.SkiaPlatform
{
    public class MainWindowImpl :
        WindowImpl
        , IWindowImpl
    {
        public MainWindowImpl()
        {
        }

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
            //this.Visibility = ViewStates.Visible;
        }

        void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot)
        {
            base.SetInputRoot(inputRoot);
            _keyboardHelper.UpdateKeyboardState(inputRoot);
        }
    }
}