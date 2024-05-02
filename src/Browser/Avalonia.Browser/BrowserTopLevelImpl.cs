using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Rendering;
using Avalonia.Browser.Skia;
using Avalonia.Browser.Storage;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;

[assembly: SupportedOSPlatform("browser")]

namespace Avalonia.Browser
{
    internal class BrowserTopLevelImpl : ITopLevelImpl
    {
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider _storageProvider;
        private readonly ISystemNavigationManagerImpl _systemNavigationManager;
        private readonly ITextInputMethodImpl _textInputMethodImpl;
        private readonly ClipboardImpl _clipboard;
        private readonly IInsetsManager _insetsManager;
        private readonly IInputPane _inputPane;
        private readonly JSObject _container;
        private readonly BrowserInputHandler _inputHandler;
        private string _currentCursor = CssCursor.Default;
        private BrowserSurface? _surface;

        static BrowserTopLevelImpl()
        {
            InputHelper.InitializeBackgroundHandlers();
        }

        public BrowserTopLevelImpl(JSObject container, JSObject nativeControlHost, JSObject inputElement)
        {
            Surfaces = Enumerable.Empty<object>();
            AcrylicCompensationLevels = new AcrylicPlatformCompensationLevels(1, 1, 1);

            _inputHandler = new BrowserInputHandler(this, container);
            _textInputMethodImpl = new BrowserTextInputMethod(_inputHandler, container, inputElement);
            _insetsManager = new BrowserInsetsManager();
            _nativeControlHost = new BrowserNativeControlHost(nativeControlHost);
            _storageProvider = new BrowserStorageProvider();
            _systemNavigationManager = new BrowserSystemNavigationManagerImpl();
            _clipboard = new ClipboardImpl();
            _inputPane = new BrowserInputPane(container);

            _container = container;

            _surface = BrowserSurface.Create(container, PixelFormats.Rgba8888);
            _surface.SizeChanged += OnSizeChanged;
            _surface.ScalingChanged += OnScalingChanged;

            Surfaces = new[] { _surface };
            Compositor = _surface.IsWebGl ?
                BrowserCompositor.WebGlUiCompositor :
                BrowserCompositor.SoftwareUiCompositor;
        }

        private void OnScalingChanged()
        {
            if (_surface is not null)
            {
                ScalingChanged?.Invoke(_surface.Scaling);
            }
        }

        private void OnSizeChanged()
        {
            if (_surface is not null)
            {
                Resized?.Invoke(_surface.ClientSize, WindowResizeReason.User);
                (_insetsManager as BrowserInsetsManager)?.NotifySafeAreaPaddingChanged();
            }
        }

        public void Dispose()
        {
            _surface?.Dispose();
            _surface = null;
        }

        public Compositor Compositor { get; }

        public void SetInputRoot(IInputRoot inputRoot) => _inputHandler.SetInputRoot(inputRoot);

        public Point PointToClient(PixelPoint point) => new(point.X, point.Y);

        public PixelPoint PointToScreen(Point point) => new((int)point.X, (int)point.Y);

        public void SetCursor(ICursorImpl? cursor)
        {
            var val = (cursor as CssCursor)?.Value ?? CssCursor.Default;
            if (_currentCursor != val)
            {
                InputHelper.SetCursor(_container, val);
                _currentCursor = val;
            }
        }

        public IPopupImpl? CreatePopup()
        {
            return null;
        }

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
        {
        }

        public Size ClientSize => _surface?.ClientSize ?? new Size(1, 1);
        public Size? FrameSize => null;
        public double RenderScaling => _surface?.Scaling ?? 1;

        public IEnumerable<object> Surfaces { get; set; }

        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
        {
            // not in the standard, but we potentially can use "apple-mobile-web-app-status-bar-style" for iOS and "theme-color" for android.
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; }

        public object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return _textInputMethodImpl;
            }

            if (featureType == typeof(ISystemNavigationManagerImpl))
            {
                return _systemNavigationManager;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IInsetsManager))
            {
                return _insetsManager;
            }

            if (featureType == typeof(IClipboard))
            {
                return _clipboard;
            }

            if (featureType == typeof(IInputPane))
            {
                return _inputPane;
            }

            return null;
        }
    }
}
