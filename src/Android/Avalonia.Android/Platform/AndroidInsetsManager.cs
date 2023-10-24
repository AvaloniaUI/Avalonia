using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.Android.Platform
{
    internal class AndroidInsetsManager : WindowInsetsAnimationCompat.Callback, IInsetsManager, IOnApplyWindowInsetsListener, ViewTreeObserver.IOnGlobalLayoutListener, ISoftwareKeyboardListener
    {
        private readonly AvaloniaMainActivity _activity;
        private readonly TopLevelImpl _topLevel;
        private bool _displayEdgeToEdge;
        private bool? _systemUiVisibility;
        private SystemBarTheme? _statusBarTheme;
        private bool? _isDefaultSystemBarLightTheme;
        private Color? _systemBarColor;
        private SoftwareKeyboardState _state;
        private float _currentSoftwareKeyboardAnimationProgress;
        private int _startHeight;
        private int _endHeight;
        private bool _isKeyboardAnimating;
        private readonly bool _usesLegacyLayouts;

        public event EventHandler<SafeAreaChangedArgs> SafeAreaChanged;
        public event EventHandler<SoftwareKeyboardStateChangedEventArgs> SoftwareKeyboardStateChanged;
        public event EventHandler SoftwareKeyboardBoundsChanged;

        public SoftwareKeyboardState SoftwareKeyboardState
        {
            get => _state; set
            {
                var oldState = _state;
                _state = value;

                if (oldState != value)
                    SoftwareKeyboardStateChanged?.Invoke(this, new SoftwareKeyboardStateChangedEventArgs(oldState, value));
            }
        }

        public bool DisplayEdgeToEdge
        {
            get => _displayEdgeToEdge;
            set
            {
                _displayEdgeToEdge = value;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                {
                    _activity.Window.Attributes.LayoutInDisplayCutoutMode = value ? LayoutInDisplayCutoutMode.ShortEdges : LayoutInDisplayCutoutMode.Default;
                }

                WindowCompat.SetDecorFitsSystemWindows(_activity.Window, !value);

                if (value)
                {
                    _activity.Window.AddFlags(WindowManagerFlags.TranslucentStatus);
                    _activity.Window.AddFlags(WindowManagerFlags.TranslucentNavigation);
                }
                else
                {
                    SystemBarColor = _systemBarColor;
                }
            }
        }

        internal AndroidInsetsManager(AvaloniaMainActivity activity, TopLevelImpl topLevel) : base(DispatchModeStop)
        {
            _activity = activity;
            _topLevel = topLevel;

            ViewCompat.SetOnApplyWindowInsetsListener(_activity.Window.DecorView, this);

            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                _usesLegacyLayouts = true;
                _activity.Window.DecorView.ViewTreeObserver.AddOnGlobalLayoutListener(this);
            }

            DisplayEdgeToEdge = false;

            ViewCompat.SetWindowInsetsAnimationCallback(_activity.Window.DecorView, this);
        }

        public Thickness SafeAreaPadding
        {
            get
            {
                var insets = ViewCompat.GetRootWindowInsets(_activity.Window.DecorView);

                if (insets != null)
                {
                    var renderScaling = _topLevel.RenderScaling;

                    var inset = insets.GetInsets(
                        _displayEdgeToEdge ?
                            WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars() |
                            WindowInsetsCompat.Type.DisplayCutout() : 0);

                    return new Thickness(inset.Left / renderScaling,
                        inset.Top / renderScaling,
                        inset.Right / renderScaling,
                        inset.Bottom / renderScaling);
                }

                return default;
            }
        }

        public Rect SoftwareKeyboardBounds
        {
            get
            {
                var insets = ViewCompat.GetRootWindowInsets(_activity.Window.DecorView);

                if (insets != null)
                {
                    float height;
                    var navbarInset = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;
                    if (_startHeight == _endHeight)
                    {
                        height = (float)((insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom - navbarInset) / _topLevel.RenderScaling);
                    }
                    else
                    {
                        var animationProgress = SoftwareKeyboardState == SoftwareKeyboardState.Open ? _currentSoftwareKeyboardAnimationProgress : 1 - _currentSoftwareKeyboardAnimationProgress;
                        height = (float)((_endHeight - navbarInset) * animationProgress / _topLevel.RenderScaling);
                    }

                    height = Math.Max(0, height); 

                    return new Rect(0, _topLevel.ClientSize.Height - SafeAreaPadding.Bottom - height, _topLevel.ClientSize.Width, height);
                }

                return default;
            }
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            insets = ViewCompat.OnApplyWindowInsets(v, insets);
            NotifySafeAreaChanged(SafeAreaPadding);

            SoftwareKeyboardState = insets.IsVisible(WindowInsetsCompat.Type.Ime()) ? SoftwareKeyboardState.Open : SoftwareKeyboardState.Closed;

            if (!_isKeyboardAnimating)
                SoftwareKeyboardBoundsChanged?.Invoke(this, EventArgs.Empty);

            return insets;
        }

        private void NotifySafeAreaChanged(Thickness safeAreaPadding)
        {
            SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(safeAreaPadding));
        }

        public void OnGlobalLayout()
        {
            NotifySafeAreaChanged(SafeAreaPadding);

            if (_usesLegacyLayouts)
            {
                var insets = ViewCompat.GetRootWindowInsets(_activity.Window.DecorView);
                SoftwareKeyboardState = insets.IsVisible(WindowInsetsCompat.Type.Ime()) ? SoftwareKeyboardState.Open : SoftwareKeyboardState.Closed;

                SoftwareKeyboardBoundsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SystemBarTheme? SystemBarTheme
        {
            get
            {
                try
                {
                    var compat = new WindowInsetsControllerCompat(_activity.Window, _topLevel.View);

                    return compat.AppearanceLightStatusBars ? Controls.Platform.SystemBarTheme.Light : Controls.Platform.SystemBarTheme.Dark;
                }
                catch (Exception _)
                {
                    return Controls.Platform.SystemBarTheme.Light;
                }
            }
            set
            {
                _statusBarTheme = value;

                if (!_topLevel.View.IsShown)
                {
                    return;
                }

                var compat = new WindowInsetsControllerCompat(_activity.Window, _topLevel.View);

                if (_isDefaultSystemBarLightTheme == null)
                {
                    _isDefaultSystemBarLightTheme = compat.AppearanceLightStatusBars;
                }

                if (value == null && _isDefaultSystemBarLightTheme != null)
                {
                    value = (bool)_isDefaultSystemBarLightTheme ? Controls.Platform.SystemBarTheme.Light : Controls.Platform.SystemBarTheme.Dark;
                }

                compat.AppearanceLightStatusBars = value == Controls.Platform.SystemBarTheme.Light;
                compat.AppearanceLightNavigationBars = value == Controls.Platform.SystemBarTheme.Light;
            }
        }

        public bool? IsSystemBarVisible
        {
            get
            {
                if (_activity.Window == null)
                {
                    return true;
                }
                var compat = ViewCompat.GetRootWindowInsets(_activity.Window.DecorView);

                return compat?.IsVisible(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
            }
            set
            {
                _systemUiVisibility = value;

                if (!_topLevel.View.IsShown)
                {
                    return;
                }

                var compat = WindowCompat.GetInsetsController(_activity.Window, _topLevel.View);

                if (value == null || value.Value)
                {
                    compat?.Show(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
                }
                else
                {
                    compat?.Hide(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());

                    if (compat != null)
                    {
                        compat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                    }
                }
            }
        }

        public Color? SystemBarColor
        {
            get => _systemBarColor;
            set
            {
                _systemBarColor = value;

                if (_systemBarColor is { } color && !_displayEdgeToEdge && _activity.Window != null)
                {
                    _activity.Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                    _activity.Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
                    _activity.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                    var androidColor = global::Android.Graphics.Color.Argb(color.A, color.R, color.G, color.B);
                    _activity.Window.SetStatusBarColor(androidColor);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        // As we can only change the navigation bar's foreground api 26 and newer, we only change the background color if running on those versions
                        _activity.Window.SetNavigationBarColor(androidColor);
                    }
                }
            }
        }

        internal void ApplyStatusBarState()
        {
            IsSystemBarVisible = _systemUiVisibility;
            SystemBarTheme = _statusBarTheme;
            SystemBarColor = _systemBarColor;
        }

        public override WindowInsetsCompat OnProgress(WindowInsetsCompat insets, IList<WindowInsetsAnimationCompat> runningAnimations)
        {
            foreach (var anim in runningAnimations)
            {
                if ((anim.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
                {
                    _currentSoftwareKeyboardAnimationProgress = anim.InterpolatedFraction;

                    SoftwareKeyboardBoundsChanged?.Invoke(this, EventArgs.Empty);

                    break;
                }
            }
            return insets;
        }

        public override WindowInsetsAnimationCompat.BoundsCompat OnStart(WindowInsetsAnimationCompat animation, WindowInsetsAnimationCompat.BoundsCompat bounds)
        {
            if ((animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
            {
                _currentSoftwareKeyboardAnimationProgress = animation.InterpolatedFraction;
                _startHeight = bounds.LowerBound.Bottom;
                _endHeight = bounds.UpperBound.Bottom;
            }

            return base.OnStart(animation, bounds);
        }

        public override void OnEnd(WindowInsetsAnimationCompat animation)
        {
            base.OnEnd(animation);

            if ((animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
            {
                _currentSoftwareKeyboardAnimationProgress = animation.InterpolatedFraction;
                _isKeyboardAnimating = false;

                _startHeight = 0;
                _endHeight = 0;

                SoftwareKeyboardBoundsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public override void OnPrepare(WindowInsetsAnimationCompat animation)
        {
            base.OnPrepare(animation);

            if ((animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
            {
                _currentSoftwareKeyboardAnimationProgress = animation.InterpolatedFraction;
                _isKeyboardAnimating = true;
            }
        }
    }
}
