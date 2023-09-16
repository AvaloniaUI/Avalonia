using System;
using System.Collections.Generic;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.X11
{
    internal class TransparencyHelper :  IDisposable
    {
        private readonly X11Info _x11;
        private readonly IntPtr _window;
        private readonly X11Globals _globals;
        private WindowTransparencyLevel _currentLevel;
        private IReadOnlyList<WindowTransparencyLevel>? _requestedLevels;
        private bool _blurAtomsAreSet;
        
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
        
        public WindowTransparencyLevel CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (_currentLevel != value)
                {
                    _currentLevel = value;
                    TransparencyLevelChanged?.Invoke(value);
                }
            }
        }

        public TransparencyHelper(X11Info x11, IntPtr window, X11Globals globals)
        {
            _x11 = x11;
            _window = window;
            _globals = globals;
            _globals.CompositionChanged += UpdateTransparency;
            _globals.WindowManagerChanged += UpdateTransparency;
        }

        public void SetTransparencyRequest(IReadOnlyList<WindowTransparencyLevel> levels)
        {
            _requestedLevels = levels;

            foreach (var level in levels)
            {
                if (!IsSupported(level))
                    continue;

                SetBlur(level == WindowTransparencyLevel.Blur);
                CurrentLevel = level;
                return;
            }

            // If we get here, we didn't find a supported level. Use the defualt of Transparent or
            // None, depending on whether composition is enabled.
            SetBlur(false);
            CurrentLevel = _globals.IsCompositionEnabled ?
                WindowTransparencyLevel.Transparent :
                WindowTransparencyLevel.None;
        }

        private bool IsSupported(WindowTransparencyLevel level)
        {
            // None is suppported when composition is disabled.
            if (level == WindowTransparencyLevel.None)
                return !_globals.IsCompositionEnabled;

            // Transparent is suppported when composition is enabled.
            if (level == WindowTransparencyLevel.Transparent)
                return _globals.IsCompositionEnabled;

            // Blur is supported when composition is enabled and KWin is used.
            if (level == WindowTransparencyLevel.Blur)
                return _globals.IsCompositionEnabled && _globals.WmName == "KWin";
            
            return false;
        }

        private void UpdateTransparency()
        {
            SetTransparencyRequest(_requestedLevels ?? Array.Empty<WindowTransparencyLevel>());
        }

        private void SetBlur(bool blur)
        {
            if (blur)
            {
                if (!_blurAtomsAreSet)
                {
                    IntPtr value = IntPtr.Zero;
                    XLib.XChangeProperty(_x11.Display, _window, _x11.Atoms._KDE_NET_WM_BLUR_BEHIND_REGION,
                        _x11.Atoms.XA_CARDINAL, 32, PropertyMode.Replace, ref value, 1);
                    _blurAtomsAreSet = true;
                }
            }
            else
            {
                if (_blurAtomsAreSet)
                {
                    XLib.XDeleteProperty(_x11.Display, _window, _x11.Atoms._KDE_NET_WM_BLUR_BEHIND_REGION);
                    _blurAtomsAreSet = false;
                }
            }
        }
        
        public void Dispose()
        {
            _globals.WindowManagerChanged -= UpdateTransparency;
            _globals.CompositionChanged -= UpdateTransparency;
        }
    }
}
