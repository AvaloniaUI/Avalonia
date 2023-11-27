﻿using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.Android.Platform
{
    internal class AndroidInsetsManager : Java.Lang.Object, IInsetsManager, IOnApplyWindowInsetsListener, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly AvaloniaMainActivity _activity;
        private readonly TopLevelImpl _topLevel;
        private readonly InsetsAnimationCallback _callback;
        private bool _displayEdgeToEdge;
        private bool _usesLegacyLayouts;
        private bool? _systemUiVisibility;
        private SystemBarTheme? _statusBarTheme;
        private bool? _isDefaultSystemBarLightTheme;
        private Color? _systemBarColor;

        public event EventHandler<SafeAreaChangedArgs> SafeAreaChanged;

        public bool DisplayEdgeToEdge
        {
            get => _displayEdgeToEdge; 
            set
            {
                _displayEdgeToEdge = value;

                var window = _activity.Window;

                if (OperatingSystem.IsAndroidVersionAtLeast(28) && window?.Attributes is { } attributes)
                {
                    attributes.LayoutInDisplayCutoutMode = value ? LayoutInDisplayCutoutMode.ShortEdges : LayoutInDisplayCutoutMode.Default;
                }

                if (window is not null)
                {
                    WindowCompat.SetDecorFitsSystemWindows(_activity.Window, !value);
                }

                if(value)
                {
                    if (window is not null)
                    {
                        window.AddFlags(WindowManagerFlags.TranslucentStatus);
                        window.AddFlags(WindowManagerFlags.TranslucentNavigation);
                    }
                }
                else
                {
                    SystemBarColor = _systemBarColor;
                }
            }
        }

        public AndroidInsetsManager(AvaloniaMainActivity activity, TopLevelImpl topLevel)
        {
            _activity = activity;
            _topLevel = topLevel;
            _callback = new InsetsAnimationCallback(WindowInsetsAnimationCompat.Callback.DispatchModeStop);

            _callback.InsetsManager = this;

            if (_activity.Window is { } window)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(window.DecorView, this);

                ViewCompat.SetWindowInsetsAnimationCallback(window.DecorView, _callback);
            }

            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                _usesLegacyLayouts = true;
                _activity.Window?.DecorView.ViewTreeObserver?.AddOnGlobalLayoutListener(this);
            }

            DisplayEdgeToEdge = false;
        }

        public Thickness SafeAreaPadding
        {
            get
            {
                var insets = _activity.Window is { } window ? ViewCompat.GetRootWindowInsets(window.DecorView) : null;

                if (insets != null)
                {
                    var renderScaling = _topLevel.RenderScaling;

                    var inset = insets.GetInsets(
                        (_displayEdgeToEdge ?
                            WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars() |
                            WindowInsetsCompat.Type.DisplayCutout() :
                            0) | WindowInsetsCompat.Type.Ime());
                    var navBarInset = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());
                    var imeInset = insets.GetInsets(WindowInsetsCompat.Type.Ime());

                    return new Thickness(inset.Left / renderScaling,
                        inset.Top / renderScaling,
                        inset.Right / renderScaling,
                        (imeInset.Bottom > 0 && ((_usesLegacyLayouts && !_displayEdgeToEdge) || !_usesLegacyLayouts) ?
                            imeInset.Bottom - (_displayEdgeToEdge ? 0 : navBarInset.Bottom) :
                            inset.Bottom) / renderScaling);
                }

                return default;
            }
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            NotifySafeAreaChanged(SafeAreaPadding);
            insets = ViewCompat.OnApplyWindowInsets(v, insets);
            return insets;
        }

        private void NotifySafeAreaChanged(Thickness safeAreaPadding)
        {
            SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(safeAreaPadding));
        }

        public void OnGlobalLayout()
        {
            NotifySafeAreaChanged(SafeAreaPadding);
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
                catch (Exception)
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

                if (value == null)
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
                if(_activity.Window == null)
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

        private class InsetsAnimationCallback : WindowInsetsAnimationCompat.Callback
        {
            public InsetsAnimationCallback(int dispatchMode) : base(dispatchMode)
            {
            }

            public AndroidInsetsManager InsetsManager { get; set; }

            public override WindowInsetsCompat OnProgress(WindowInsetsCompat insets, IList<WindowInsetsAnimationCompat> runningAnimations)
            {
                foreach (var anim in runningAnimations)
                {
                    if ((anim.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
                    {
                        var renderScaling = InsetsManager._topLevel.RenderScaling;

                        var inset = insets.GetInsets((InsetsManager.DisplayEdgeToEdge ? WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars() | WindowInsetsCompat.Type.DisplayCutout() : 0) | WindowInsetsCompat.Type.Ime());
                        var navBarInset = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());
                        var imeInset = insets.GetInsets(WindowInsetsCompat.Type.Ime());


                        var bottomPadding = (imeInset.Bottom > 0 && !InsetsManager.DisplayEdgeToEdge ? imeInset.Bottom - navBarInset.Bottom : inset.Bottom);
                        bottomPadding = (int)(bottomPadding * anim.InterpolatedFraction);

                        var padding = new Thickness(inset.Left / renderScaling,
                            inset.Top / renderScaling,
                            inset.Right / renderScaling,
                            bottomPadding / renderScaling);
                        InsetsManager?.NotifySafeAreaChanged(padding);
                        break;
                    }
                }
                return insets;
            }
        }
    }
}
