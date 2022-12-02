using Avalonia.Controls.Platform;
using ControlCatalog.Pages;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class PlatformInsetsPageViewModel : ViewModelBase
    {
        private bool _useSafeArea;
        private bool _lightThemeSystemBar;
        private bool _systemBarDefault;
        private bool _systemBarDrawBehind;
        private bool _systemBarHide;
        private WindowInsetsPage _page;

        public PlatformInsetsPageViewModel(WindowInsetsPage page)
        {
            _page = page;
        }

        public bool UseSafeArea
        {
            get => _useSafeArea;
            set => this.RaiseAndSetIfChanged(ref _useSafeArea, value);
        }

        public bool LightThemeSystemBar
        {
            get => _lightThemeSystemBar;
            set
            {
                this.RaiseAndSetIfChanged(ref _lightThemeSystemBar, value);

                _page.SystemBarTheme = value ? SystemBarTheme.Light : SystemBarTheme.Dark;
            }
        }

        public bool SystemBarDefault
        {
            get => _systemBarDefault;
            set
            {
                this.RaiseAndSetIfChanged(ref _systemBarDefault, value);

                if(value)
                {
                    _page.WindowState = Avalonia.Controls.WindowState.Normal;
                }
            }
        }

        public bool SystemBarDrawBehind
        {
            get => _systemBarDrawBehind;
            set
            {
                this.RaiseAndSetIfChanged(ref _systemBarDrawBehind, value);

                if(value)
                {
                    _page.WindowState = Avalonia.Controls.WindowState.Maximized;
                }
            }
        }

        public bool SystemBarHide
        {
            get => _systemBarHide;
            set
            {
                this.RaiseAndSetIfChanged(ref _systemBarHide, value);

                if(value)
                {
                    _page.WindowState = Avalonia.Controls.WindowState.FullScreen;
                }
            }
        }
    }
}
