using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MiniMvvm;

namespace SafeAreaDemo.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _useSafeArea = true;
        private bool _displayEdgeToEdge;
        private IInsetsManager? _insetsManager;
        private IInsetsWithCaptionManager? _captionManager;
        private bool _hideSystemBars;
        private bool _autoSafeAreaPadding;
        private IInputPane? _inputPane;
        private bool _extendClientIntoCaption;
        public event EventHandler? SafeAreaChanged;

        public InputPaneState InputPaneState
        {
            get
            {
                return _inputPane?.State ?? InputPaneState.Closed;
            }
        }

        public IEasing? InputPaneEasing { get; private set; }
        public TimeSpan? InputPaneDuration { get; private set; }

        public Thickness InputPaneMarkerMargin => InputPaneState == InputPaneState.Open
            ? new Thickness(0, 0, 0, Math.Max(0, CanvasSize.Height - InputPaneRect.Top))
            : default;
        public Rect InputPaneRect => _inputPane?.OccludedRect ?? default;

        public Rect CanvasSize { get; set; }

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

        public Thickness CaptionPadding { get; set; } = default;
        public double CaptionHeight { get; set; } = default;

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
                    _insetsManager.DisplayEdgeToEdgePreference = value;
                }

                this.RaisePropertyChanged();

                RaiseSafeAreaChanged();
            }
        }

        public bool ExtendClientIntoCaption
        {
            get => _extendClientIntoCaption && HasCaptionBar;
            set
            {
                _extendClientIntoCaption = value;
                if (_captionManager != null)
                {
                    _captionManager.ExtendClientIntoCaption = value;
                }
                this.RaisePropertyChanged();
                RaiseSafeAreaChanged();
            }
        }

        public bool HasCaptionBar => _captionManager != null && _captionManager.IsCaptionBarVisible;

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

        internal void Initialize(Control mainView, IInsetsManager? InsetsManager, IInputPane? inputPane)
        {
            if (_insetsManager != null)
            {
                _insetsManager.SafeAreaChanged -= InsetsManager_SafeAreaChanged;
            }
            if (_inputPane != null)
            {
                _inputPane.StateChanged -= InputPaneOnStateChanged;
            }

            _autoSafeAreaPadding = mainView.GetValue(TopLevel.AutoSafeAreaPaddingProperty);
            _insetsManager = InsetsManager;
            _captionManager = InsetsManager as IInsetsWithCaptionManager;


            if (_insetsManager != null)
            {
                _insetsManager.SafeAreaChanged += InsetsManager_SafeAreaChanged;

                _displayEdgeToEdge = _insetsManager.DisplayEdgeToEdgePreference;
                _hideSystemBars = !(_insetsManager.IsSystemBarVisible ?? false);
            }

            if (_captionManager != null)
            {
                CaptionPadding = _captionManager.CaptionBarPadding;
                CaptionHeight = _captionManager.CaptionBarHeight;
                _extendClientIntoCaption = _captionManager.ExtendClientIntoCaption;
            }

            _inputPane = inputPane;
            if (_inputPane != null)
            {
                _inputPane.StateChanged += InputPaneOnStateChanged;
            }

            RaiseKeyboardChanged();
        }

        private void InputPaneOnStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            InputPaneDuration = e.AnimationDuration;
            InputPaneEasing = e.Easing ?? new LinearEasing();
            RaiseKeyboardChanged();
        }

        private void InsetsManager_SafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        {
            if (_captionManager != null)
            {
                CaptionPadding = _captionManager.CaptionBarPadding;
                CaptionHeight = _captionManager.CaptionBarHeight;
            }
            RaiseSafeAreaChanged();
        }

        private void RaiseSafeAreaChanged()
        {
            this.RaisePropertyChanged(nameof(SafeAreaPadding));
            this.RaisePropertyChanged(nameof(ViewPadding));
            this.RaisePropertyChanged(nameof(InputPaneMarkerMargin));
            this.RaisePropertyChanged(nameof(HasCaptionBar));
            this.RaisePropertyChanged(nameof(CaptionHeight));
            this.RaisePropertyChanged(nameof(CaptionPadding));
            SafeAreaChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseKeyboardChanged()
        {
            this.RaisePropertyChanged(nameof(InputPaneState));
            this.RaisePropertyChanged(nameof(InputPaneRect));
            this.RaisePropertyChanged(nameof(InputPaneEasing));
            this.RaisePropertyChanged(nameof(InputPaneDuration));
            this.RaisePropertyChanged(nameof(InputPaneMarkerMargin));
        }

        public void SetLight()
        {
            if (Application.Current is { } application)
            {
                application.RequestedThemeVariant = ThemeVariant.Light;
            }
        }

        public void SetDark()
        {
            if (Application.Current is { } application)
            {
                application.RequestedThemeVariant = ThemeVariant.Dark;
            }
        }

        public void SetDefault()
        {
            if (Application.Current is { } application)
            {
                application.RequestedThemeVariant = ThemeVariant.Default;
            }
        }
    }
}
