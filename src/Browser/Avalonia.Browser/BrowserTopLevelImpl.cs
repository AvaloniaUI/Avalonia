using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
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
        private Size _clientSize;
        private IInputRoot? _inputRoot;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly AvaloniaView _avaloniaView;
        private readonly TouchDevice _touchDevice;
        private readonly PenDevice _penDevice;
        private string _currentCursor = CssCursor.Default;
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider _storageProvider;
        private readonly ISystemNavigationManagerImpl _systemNavigationManager;
        private readonly ClipboardImpl _clipboard;
        private readonly IInsetsManager? _insetsManager;
        private readonly IInputPane _inputPane;

        public BrowserTopLevelImpl(AvaloniaView avaloniaView, JSObject container)
        {
            Surfaces = Enumerable.Empty<object>();
            _avaloniaView = avaloniaView;
            AcrylicCompensationLevels = new AcrylicPlatformCompensationLevels(1, 1, 1);
            _touchDevice = new TouchDevice();
            _penDevice = new PenDevice();

            _insetsManager = new BrowserInsetsManager();
            _nativeControlHost = _avaloniaView.GetNativeControlHostImpl();
            _storageProvider = new BrowserStorageProvider();
            _systemNavigationManager = new BrowserSystemNavigationManagerImpl();
            _clipboard = new ClipboardImpl();
            _inputPane = new BrowserInputPane(container);
        }

        public ulong Timestamp => (ulong)_sw.ElapsedMilliseconds;

        public void SetClientSize(Size newSize, double dpi)
        {
            if (Math.Abs(RenderScaling - dpi) > 0.0001)
            {
                if (Surfaces.FirstOrDefault() is BrowserSkiaSurface surface)
                {
                    surface.Scaling = dpi;
                }
                
                ScalingChanged?.Invoke(dpi);
            }

            if (newSize != _clientSize)
            {
                _clientSize = newSize;

                if (Surfaces.FirstOrDefault() is BrowserSkiaSurface surface)
                {
                    surface.Size = new PixelSize((int)newSize.Width, (int)newSize.Height);
                }

                Resized?.Invoke(newSize, WindowResizeReason.User);

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
                var device = GetPointerDevice(pointerType);
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

        private IPointerDevice GetPointerDevice(string pointerType)
        {
            return pointerType switch
            {
                "touch" => _touchDevice,
                "pen" => _penDevice,
                _ => MouseDevice
            };
        }

        public bool RawMouseWheelEvent(Point p, Vector v, RawInputModifiers modifiers)
        {
            if (_inputRoot is { })
            {
                var args = new RawMouseWheelEventArgs(MouseDevice, Timestamp, _inputRoot, p, v, modifiers);
                
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

        }

        public Compositor Compositor { get; } = new(AvaloniaLocator.Current.GetRequiredService<IPlatformGraphics>());

        public void Invalidate(Rect rect)
        {
            //Console.WriteLine("invalidate rect called");
        }

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
                SetCssCursor?.Invoke(val);
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

        public Size ClientSize => _clientSize;
        public Size? FrameSize => null;
        public double RenderScaling => (Surfaces.FirstOrDefault() as BrowserSkiaSurface)?.Scaling ?? 1;

        public IEnumerable<object> Surfaces { get; set; }

        public Action<string>? SetCssCursor { get; set; }
        public Action<RawInputEventArgs>? Input { get; set; }
        public Action<Rect>? Paint { get; set; }
        public Action<Size, WindowResizeReason>? Resized { get; set; }
        public Action<double>? ScalingChanged { get; set; }
        public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
        public Action? Closed { get; set; }
        public Action? LostFocus { get; set; }
        public IMouseDevice MouseDevice { get; } = new MouseDevice();

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
                return _avaloniaView;
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
