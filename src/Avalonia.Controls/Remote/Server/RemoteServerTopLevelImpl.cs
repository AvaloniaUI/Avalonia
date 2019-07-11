using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls.Embedding.Offscreen;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;
using InputModifiers = Avalonia.Input.InputModifiers;
using Key = Avalonia.Input.Key;
using PixelFormat = Avalonia.Platform.PixelFormat;
using ProtocolPixelFormat = Avalonia.Remote.Protocol.Viewport.PixelFormat;

namespace Avalonia.Controls.Remote.Server
{
    public class RemoteServerTopLevelImpl : OffscreenTopLevelImplBase, IFramebufferPlatformSurface
    {
        private readonly IAvaloniaRemoteTransportConnection _transport;
        private LockedFramebuffer _framebuffer;
        private object _lock = new object();
        private long _lastSentFrame = -1;
        private long _lastReceivedFrame = -1;
        private long _nextFrameNumber = 1;
        private ClientViewportAllocatedMessage _pendingAllocation;
        private bool _invalidated;
        private Vector _dpi = new Vector(96, 96);
        private ProtocolPixelFormat[] _supportedFormats;

        public RemoteServerTopLevelImpl(IAvaloniaRemoteTransportConnection transport)
        {
            _transport = transport;
            _transport.OnMessage += OnMessage;

            KeyboardDevice = AvaloniaLocator.Current.GetService<IKeyboardDevice>();
        }

        private static RawPointerEventType GetAvaloniaEventType (Avalonia.Remote.Protocol.Input.MouseButton button, bool pressed)
        {
            switch (button)
            {
                case Avalonia.Remote.Protocol.Input.MouseButton.Left:
                    return pressed ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp;

                case Avalonia.Remote.Protocol.Input.MouseButton.Middle:
                    return pressed ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp;

                case Avalonia.Remote.Protocol.Input.MouseButton.Right:
                    return pressed ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp;

                default:
                    return RawPointerEventType.Move;
            }
        }

        private static InputModifiers GetAvaloniaInputModifiers (Avalonia.Remote.Protocol.Input.InputModifiers[] modifiers)
        {
            var result = InputModifiers.None;

            if (modifiers == null)
            {
                return result;
            }

            foreach(var modifier in modifiers)
            {
                switch (modifier)
                {
                    case Avalonia.Remote.Protocol.Input.InputModifiers.Control:
                        result |= InputModifiers.Control;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.Alt:
                        result |= InputModifiers.Alt;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.Shift:
                        result |= InputModifiers.Shift;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.Windows:
                        result |= InputModifiers.Windows;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.LeftMouseButton:
                        result |= InputModifiers.LeftMouseButton;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.MiddleMouseButton:
                        result |= InputModifiers.MiddleMouseButton;
                        break;

                    case Avalonia.Remote.Protocol.Input.InputModifiers.RightMouseButton:
                        result |= InputModifiers.RightMouseButton;
                        break;
                }
            }

            return result;
        }

        protected virtual void OnMessage(IAvaloniaRemoteTransportConnection transport, object obj)
        {
            lock (_lock)
            {
                if (obj is FrameReceivedMessage lastFrame)
                {
                    lock (_lock)
                    {
                        _lastReceivedFrame = lastFrame.SequenceId;
                    }
                    Dispatcher.UIThread.Post(RenderIfNeeded);
                }
                if(obj is ClientRenderInfoMessage renderInfo)
                {
                    lock(_lock)
                    {
                        _dpi = new Vector(renderInfo.DpiX, renderInfo.DpiY);
                        _invalidated = true;
                    }
                    
                    Dispatcher.UIThread.Post(RenderIfNeeded);
                }
                if (obj is ClientSupportedPixelFormatsMessage supportedFormats)
                {
                    lock (_lock)
                        _supportedFormats = supportedFormats.Formats;
                    Dispatcher.UIThread.Post(RenderIfNeeded);
                }
                if (obj is MeasureViewportMessage measure)
                    Dispatcher.UIThread.Post(() =>
                    {
                        var m = Measure(new Size(measure.Width, measure.Height));
                        _transport.Send(new MeasureViewportMessage
                        {
                            Width = m.Width,
                            Height = m.Height
                        });
                    });
                if (obj is ClientViewportAllocatedMessage allocated)
                {
                    lock (_lock)
                    {
                        if (_pendingAllocation == null)
                            Dispatcher.UIThread.Post(() =>
                            {
                                ClientViewportAllocatedMessage allocation;
                                lock (_lock)
                                {
                                    allocation = _pendingAllocation;
                                    _pendingAllocation = null;
                                }
                                _dpi = new Vector(allocation.DpiX, allocation.DpiY);
                                ClientSize = new Size(allocation.Width, allocation.Height);
                                RenderIfNeeded();
                            });

                        _pendingAllocation = allocated;
                    }
                }
                if(obj is PointerMovedEventMessage pointer)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Input?.Invoke(new RawPointerEventArgs(
                            MouseDevice, 
                            0, 
                            InputRoot, 
                            RawPointerEventType.Move, 
                            new Point(pointer.X, pointer.Y), 
                            GetAvaloniaInputModifiers(pointer.Modifiers)));
                    }, DispatcherPriority.Input);
                }
                if(obj is PointerPressedEventMessage pressed)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Input?.Invoke(new RawPointerEventArgs(
                            MouseDevice,
                            0,
                            InputRoot,
                            GetAvaloniaEventType(pressed.Button, true),
                            new Point(pressed.X, pressed.Y),
                            GetAvaloniaInputModifiers(pressed.Modifiers)));
                    }, DispatcherPriority.Input);
                }
                if (obj is PointerPressedEventMessage released)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Input?.Invoke(new RawPointerEventArgs(
                            MouseDevice,
                            0,
                            InputRoot,
                            GetAvaloniaEventType(released.Button, false),
                            new Point(released.X, released.Y),
                            GetAvaloniaInputModifiers(released.Modifiers)));
                    }, DispatcherPriority.Input);
                }
                if(obj is ScrollEventMessage scroll)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Input?.Invoke(new RawMouseWheelEventArgs(
                            MouseDevice,
                            0,
                            InputRoot,
                            new Point(scroll.X, scroll.Y),
                            new Vector(scroll.DeltaX, scroll.DeltaY),
                            GetAvaloniaInputModifiers(scroll.Modifiers)));
                    }, DispatcherPriority.Input);
                }
                if(obj is KeyEventMessage key)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                        Input?.Invoke(new RawKeyEventArgs(
                            KeyboardDevice,
                            0,
                            key.IsDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                            (Key)key.Key,
                            GetAvaloniaInputModifiers(key.Modifiers)));
                    }, DispatcherPriority.Input);
                }
                if(obj is TextInputEventMessage text)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                        Input?.Invoke(new RawTextInputEventArgs(
                            KeyboardDevice,
                            0,
                            text.Text));
                    }, DispatcherPriority.Input);
                }
            }
        }

        protected void SetDpi(Vector dpi)
        {
            _dpi = dpi;
            RenderIfNeeded();
        }

        protected virtual Size Measure(Size constraint)
        {
            var l = (ILayoutable) InputRoot;
            l.Measure(constraint);
            return l.DesiredSize;
        }

        public override IEnumerable<object> Surfaces => new[] { this };
        
        FrameMessage RenderFrame(int width, int height, ProtocolPixelFormat? format)
        {
            var scalingX = _dpi.X / 96.0;
            var scalingY = _dpi.Y / 96.0;

            width = (int)(width * scalingX);
            height = (int)(height * scalingY);

            var fmt = format ?? ProtocolPixelFormat.Rgba8888;
            var bpp = fmt == ProtocolPixelFormat.Rgb565 ? 2 : 4;
            var data = new byte[width * height * bpp];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                if (width > 0 && height > 0)
                {
                    _framebuffer = new LockedFramebuffer(handle.AddrOfPinnedObject(), new PixelSize(width, height), width * bpp, _dpi, (PixelFormat)fmt,
                        null);
                    Paint?.Invoke(new Rect(0, 0, width, height));
                }
            }
            finally
            {
                _framebuffer = null;
                handle.Free();
            }
            return new FrameMessage
            {
                Data = data,
                Format = (ProtocolPixelFormat) format,
                Width = width,
                Height = height,
                Stride = width * bpp,
            };
        }

        public ILockedFramebuffer Lock()
        {
            if (_framebuffer == null)
                throw new InvalidOperationException("Paint was not requested, wait for Paint event");
            return _framebuffer;
        }

        protected void RenderIfNeeded()
        {
            lock (_lock)
            {
                if (_lastReceivedFrame != _lastSentFrame || !_invalidated || _supportedFormats == null)
                    return;

            }

            var format = ProtocolPixelFormat.Rgba8888;
            foreach(var fmt in _supportedFormats)
                if (fmt <= ProtocolPixelFormat.MaxValue)
                {
                    format = fmt;
                    break;
                }
            
            var frame = RenderFrame((int) ClientSize.Width, (int) ClientSize.Height, format);
            lock (_lock)
            {
                _lastSentFrame = _nextFrameNumber++;
                frame.SequenceId = _lastSentFrame;
                _invalidated = false;
            }
            _transport.Send(frame);
        }

        public override void Invalidate(Rect rect)
        {
            if (!IsDisposed)
            {
                _invalidated = true;
                Dispatcher.UIThread.Post(RenderIfNeeded);
            }
        }

        public override IMouseDevice MouseDevice { get; } = new MouseDevice();

        public IKeyboardDevice KeyboardDevice { get; }
    }
}
