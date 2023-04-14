using Avalonia;
using Avalonia.Controls.Platform;
using MiniMvvm;

namespace SafeAreaDemo.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _useSafeArea = true;
        private bool _fullscreen;
        private IInsetsManager? _insetsManager;
        private bool _hideSystemBars;

        public Thickness SafeAreaPadding
        {
            get
            {
                return _insetsManager?.SafeAreaPadding ?? default;
            }
        }

        public Thickness ViewPadding
        {
            get
            {
                return _useSafeArea ? SafeAreaPadding : default;
            }
        }

        public bool UseSafeArea
        {
            get => _useSafeArea;
            set
            {
                _useSafeArea = value;

                this.RaisePropertyChanged();

                RaiseSafeAreaChanged();
            }
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set
            {
                _fullscreen = value;

                if (_insetsManager != null)
                {
                    _insetsManager.DisplayEdgeToEdge = value;
                }

                this.RaisePropertyChanged();

                RaiseSafeAreaChanged();
            }
        }

        public bool HideSystemBars
        {
            get => _hideSystemBars;
            set
            {
                _hideSystemBars = value;

                if (_insetsManager != null)
                {
                    _insetsManager.IsSystemBarVisible = !value;
                }

                this.RaisePropertyChanged();

                RaiseSafeAreaChanged();
            }
        }

        internal IInsetsManager? InsetsManager
        {
            get => _insetsManager; 
            set
            {
                if (_insetsManager != null)
                {
                    _insetsManager.SafeAreaChanged -= InsetsManager_SafeAreaChanged;
                }

                _insetsManager = value;

                if (_insetsManager != null)
                {
                    _insetsManager.SafeAreaChanged += InsetsManager_SafeAreaChanged;

                    _insetsManager.DisplayEdgeToEdge = _fullscreen;
                    _insetsManager.IsSystemBarVisible = !_hideSystemBars;
                }
            }
        }

        private void InsetsManager_SafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        {
            RaiseSafeAreaChanged();
        }

        private void RaiseSafeAreaChanged()
        {
            this.RaisePropertyChanged(nameof(SafeAreaPadding));
            this.RaisePropertyChanged(nameof(ViewPadding));
        }
    }
}
