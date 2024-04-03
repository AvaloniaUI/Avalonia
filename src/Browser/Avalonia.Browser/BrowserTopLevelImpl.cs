using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

[assembly: SupportedOSPlatform("browser")]

namespace Avalonia.Browser
{
    internal class BrowserTopLevelImpl : ITopLevelImpl
    {
        private IInputRoot? _inputRoot;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly TouchDevice _touchDevice;
        private readonly PenDevice _penDevice;
        private BrowserSurface? _surface;
        private string _currentCursor = CssCursor.Default;
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider _storageProvider;
        private readonly ISystemNavigationManagerImpl _systemNavigationManager;
        private readonly ITextInputMethodImpl _textInputMethodImpl;
        private readonly ClipboardImpl _clipboard;
        private readonly IInsetsManager _insetsManager;
        private readonly IInputPane _inputPane;
        private readonly List<BrowserMouseDevice> _mouseDevices;
        private readonly JSObject _container;

        public BrowserTopLevelImpl(JSObject container, JSObject nativeControlHost, ITextInputMethodImpl textInputMethodImpl)
        {
            _textInputMethodImpl = textInputMethodImpl;
            Surfaces = Enumerable.Empty<object>();
            AcrylicCompensationLevels = new AcrylicPlatformCompensationLevels(1, 1, 1);
            _touchDevice = new TouchDevice();
            _penDevice = new PenDevice();

            _insetsManager = new BrowserInsetsManager();
            _nativeControlHost = new BrowserNativeControlHost(nativeControlHost);
            _storageProvider = new BrowserStorageProvider();
            _systemNavigationManager = new BrowserSystemNavigationManagerImpl();
            _clipboard = new ClipboardImpl();
            _inputPane = new BrowserInputPane(container);

            _mouseDevices = new();
            _container = container;

            _surface = BrowserSurface.Create(container, PixelFormats.Rgba8888);
            _surface.SizeChanged += OnSizeChanged;
            _surface.ScalingChanged += OnScalingChanged;
            Surfaces = new[] { _surface };
            Compositor = _surface.IsWebGl ?
                BrowserCompositor.WebGlUiCompositor :
                BrowserCompositor.SoftwareUiCompositor;
        }

        public ulong Timestamp => (ulong)_sw.ElapsedMilliseconds;

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

        public bool RawPointerEvent(
            RawPointerEventType eventType, string pointerType,
            RawPointerPoint p, RawInputModifiers modifiers, long touchPointId,
            Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints = null)
        {
            if (_inputRoot is { }
                && Input is { } input)
            {
                var device = GetPointerDevice(pointerType, touchPointId);
                var args = device is TouchDevice ?
                    new RawTouchEventArgs(device, Timestamp, _inputRoot, eventType, p, modifiers, touchPointId)
                    {
                        IntermediatePoints = intermediatePoints
                    } :
                    new RawPointerEventArgs(device, Timestamp, _inputRoot, eventType, p, modifiers)
                    {
                        RawPointerId = touchPointId,
                        IntermediatePoints = intermediatePoints
                    };

                input.Invoke(args);

                return args.Handled;
            }

            return false;
        }

        private IPointerDevice GetPointerDevice(string pointerType, long pointerId)
        {
            if (pointerType == "touch")
                return _touchDevice;
            else if (pointerType == "pen")
                return _penDevice;

            // TODO: refactor pointer devices, so we can reuse single instance here.
            foreach (var mouseDevice in _mouseDevices)
            {
                if (mouseDevice.PointerId == pointerId)
                    return mouseDevice;
            }
            var newMouseDevice = new BrowserMouseDevice(pointerId, _container);
            _mouseDevices.Add(newMouseDevice);
            return newMouseDevice;
        }

        public bool RawMouseWheelEvent(Point p, Vector v, RawInputModifiers modifiers)
        {
            if (_inputRoot is { })
            {
                var args = new RawMouseWheelEventArgs(WheelMouseDevice, Timestamp, _inputRoot, p, v, modifiers);
                
                Input?.Invoke(args);

                return args.Handled;
            }

            return false;
        }

        public bool RawKeyboardEvent(RawKeyEventType type, string domCode, string domKey, RawInputModifiers modifiers)
        {
            if (_inputRoot is null)
                return false;

            var physicalKey = KeyInterop.PhysicalKeyFromDomCode(domCode);
            var key = KeyInterop.KeyFromDomKey(domKey, physicalKey);
            var keySymbol = KeyInterop.KeySymbolFromDomKey(domKey);

            var args = new RawKeyEventArgs(
                KeyboardDevice,
                Timestamp,
                _inputRoot,
                type,
                key,
                modifiers,
                physicalKey,
                keySymbol
            );

            Input?.Invoke(args);

            return args.Handled;
        }

        public bool RawTextEvent(string text)
        {
            if (_inputRoot is { })
            {
                var args = new RawTextInputEventArgs(KeyboardDevice, Timestamp, _inputRoot, text);
                Input?.Invoke(args);

                return args.Handled;
            }

            return false;
        }
        
        public DragDropEffects RawDragEvent(RawDragEventType eventType, Point position, RawInputModifiers modifiers, BrowserDataObject dataObject, DragDropEffects dropEffect)
        {
            var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
            var eventArgs = new RawDragEvent(device, eventType, _inputRoot!, position, dataObject, dropEffect, modifiers);
            Console.WriteLine($"{eventArgs.Location} {eventArgs.Effects} {eventArgs.Type} {eventArgs.KeyModifiers}");
            Input?.Invoke(eventArgs);
            return eventArgs.Effects;
        }

        public void Dispose()
        {
            _surface?.Dispose();
            _surface = null;
        }

        public Compositor Compositor { get; }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot;
        }

        public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

        public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

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
        public IMouseDevice WheelMouseDevice { get; } = new MouseDevice();

        public IKeyboardDevice KeyboardDevice { get; } = BrowserWindowingPlatform.Keyboard;
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
