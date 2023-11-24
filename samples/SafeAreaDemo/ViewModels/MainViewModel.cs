using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using MiniMvvm;

namespace SafeAreaDemo.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _useSafeArea = true;
        private bool _displayEdgeToEdge;
        private IInsetsManager? _insetsManager;
        private bool _hideSystemBars;
        private bool _autoSafeAreaPadding;

        public Thickness SafeAreaPadding
        {
            get
            {
                return !_autoSafeAreaPadding ? _insetsManager?.SafeAreaPadding ?? default : default;
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

        public bool DisplayEdgeToEdge
        {
            get => _displayEdgeToEdge;
            set
            {
                _displayEdgeToEdge = value;

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

        public bool AutoSafeAreaPadding
        {
            get => _autoSafeAreaPadding;
            set
            {
                _autoSafeAreaPadding = value;
                
                RaisePropertyChanged();
                RaiseSafeAreaChanged();
            }
        }

        internal void Initialize(Control mainView, IInsetsManager? InsetsManager)
        {
            if (_insetsManager != null)
            {
                _insetsManager.SafeAreaChanged -= InsetsManager_SafeAreaChanged;
            }

            _autoSafeAreaPadding = mainView.GetValue(TopLevel.AutoSafeAreaPaddingProperty);
            _insetsManager = InsetsManager;

            if (_insetsManager != null)
            {
                _insetsManager.SafeAreaChanged += InsetsManager_SafeAreaChanged;

                _displayEdgeToEdge = _insetsManager.DisplayEdgeToEdge;
                _hideSystemBars = !(_insetsManager.IsSystemBarVisible ?? false);
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
