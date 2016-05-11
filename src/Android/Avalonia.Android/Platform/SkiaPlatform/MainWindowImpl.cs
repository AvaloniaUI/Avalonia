using Android.Views;
using Avalonia.Android.Platform.Specific;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    public class MainWindowImpl :
        WindowImpl
        , IWindowImpl
    {
        public MainWindowImpl()
        {
        }

        public WindowState WindowState
        {
            get { return WindowState.Normal; }
            set { }
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
            AvaloniaLocator.Current.GetService<IAndroidActivity>().ContentView = this;
            //this.Visibility = ViewStates.Visible;
        }

        void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot)
        {
            base.SetInputRoot(inputRoot);
            _keyboardHelper.UpdateKeyboardState(inputRoot);
        }
    }
}