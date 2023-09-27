using System;
using System.Collections.Generic;
using Avalonia.Controls.Embedding.Offscreen;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;
using Key = Avalonia.Input.Key;
using PhysicalKey = Avalonia.Input.PhysicalKey;
using ProtocolPixelFormat = Avalonia.Remote.Protocol.Viewport.PixelFormat;
using ProtocolMouseButton = Avalonia.Remote.Protocol.Input.MouseButton;

namespace Avalonia.Controls.Remote.Server
{
    [Unstable]
    internal partial class RemoteServerTopLevelImpl : OffscreenTopLevelImplBase, IFramebufferPlatformSurface, ITopLevelImpl
    {
        private readonly IAvaloniaRemoteTransportConnection _transport;
        private readonly object _lock = new();
        private readonly Action _sendLastFrameIfNeeded;
        private readonly Action _renderAndSendFrameIfNeeded;
        private Framebuffer _framebuffer = Framebuffer.Empty;
        private long _lastSentFrame = -1;
        private long _lastReceivedFrame = -1;
        private long _nextFrameNumber = 1;
        private ClientViewportAllocatedMessage? _pendingAllocation;
        private ProtocolPixelFormat? _format;

        public RemoteServerTopLevelImpl(IAvaloniaRemoteTransportConnection transport)
        {
            _sendLastFrameIfNeeded = SendLastFrameIfNeeded;
            _renderAndSendFrameIfNeeded = RenderAndSendFrameIfNeeded;

            _transport = transport;
            _transport.OnMessage += OnMessage;

            KeyboardDevice = AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
        }

        private static RawPointerEventType GetAvaloniaEventType(ProtocolMouseButton button, bool pressed)
        {
            switch (button)
            {
                case ProtocolMouseButton.Left:
                    return pressed ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp;

                case ProtocolMouseButton.Middle:
                    return pressed ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp;

                case ProtocolMouseButton.Right:
                    return pressed ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp;

                default:
                    return RawPointerEventType.Move;
            }
        }

        private static RawInputModifiers GetAvaloniaRawInputModifiers(InputModifiers[]? modifiers)
        {
            var result = RawInputModifiers.None;

            if (modifiers == null)
            {
                return result;
            }

            foreach(var modifier in modifiers)
            {
                switch (modifier)
                {
                    case InputModifiers.Control:
                        result |= RawInputModifiers.Control;
                        break;

                    case InputModifiers.Alt:
                        result |= RawInputModifiers.Alt;
                        break;

                    case InputModifiers.Shift:
                        result |= RawInputModifiers.Shift;
                        break;

                    case InputModifiers.Windows:
                        result |= RawInputModifiers.Meta;
                        break;

                    case InputModifiers.LeftMouseButton:
                        result |= RawInputModifiers.LeftMouseButton;
                        break;

                    case InputModifiers.MiddleMouseButton:
                        result |= RawInputModifiers.MiddleMouseButton;
                        break;

                    case InputModifiers.RightMouseButton:
                        result |= RawInputModifiers.RightMouseButton;
                        break;
                }
            }

            return result;
        }

        protected virtual void OnMessage(IAvaloniaRemoteTransportConnection transport, object obj)
        {
            lock (_lock)
            {
                switch (obj)
                {
                    case FrameReceivedMessage lastFrame:
                        _lastReceivedFrame = Math.Max(lastFrame.SequenceId, _lastReceivedFrame);
                        Dispatcher.UIThread.Post(_sendLastFrameIfNeeded);
                        break;

                    case ClientRenderInfoMessage renderInfo:
                        Dispatcher.UIThread.Post(() =>
                        {
                            RenderScaling = renderInfo.DpiX / 96.0;
                            RenderAndSendFrameIfNeeded();
                        });
                        break;

                    case ClientSupportedPixelFormatsMessage supportedFormats:
                        _format = TryGetValidPixelFormat(supportedFormats.Formats);
                        Dispatcher.UIThread.Post(_renderAndSendFrameIfNeeded);
                        break;

                    case MeasureViewportMessage measure:
                        Dispatcher.UIThread.Post(() =>
                        {
                            var m = Measure(new Size(measure.Width, measure.Height));
                            _transport.Send(new MeasureViewportMessage
                            {
                                Width = m.Width,
                                Height = m.Height
                            });
                        });
                        break;

                    case ClientViewportAllocatedMessage allocated:
                        if (_pendingAllocation == null)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                ClientViewportAllocatedMessage allocation;
                                lock (_lock)
                                {
                                    allocation = _pendingAllocation!;
                                    _pendingAllocation = null;
                                }

                                RenderScaling = allocation.DpiX / 96.0;
                                ClientSize = new Size(allocation.Width, allocation.Height);
                                RenderAndSendFrameIfNeeded();
                            });
                        }

                        _pendingAllocation = allocated;
                        break;

                    case PointerMovedEventMessage pointer:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Input?.Invoke(new RawPointerEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                RawPointerEventType.Move,
                                new Point(pointer.X, pointer.Y),
                                GetAvaloniaRawInputModifiers(pointer.Modifiers)));
                        }, DispatcherPriority.Input);
                        break;

                    case PointerPressedEventMessage pressed:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Input?.Invoke(new RawPointerEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                GetAvaloniaEventType(pressed.Button, true),
                                new Point(pressed.X, pressed.Y),
                                GetAvaloniaRawInputModifiers(pressed.Modifiers)));
                        }, DispatcherPriority.Input);
                        break;

                    case PointerReleasedEventMessage released:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Input?.Invoke(new RawPointerEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                GetAvaloniaEventType(released.Button, false),
                                new Point(released.X, released.Y),
                                GetAvaloniaRawInputModifiers(released.Modifiers)));
                        }, DispatcherPriority.Input);
                        break;

                    case ScrollEventMessage scroll:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Input?.Invoke(new RawMouseWheelEventArgs(
                                MouseDevice,
                                0,
                                InputRoot!,
                                new Point(scroll.X, scroll.Y),
                                new Vector(scroll.DeltaX, scroll.DeltaY),
                                GetAvaloniaRawInputModifiers(scroll.Modifiers)));
                        }, DispatcherPriority.Input);
                        break;

                    case KeyEventMessage key:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                            Input?.Invoke(new RawKeyEventArgs(
                                KeyboardDevice,
                                0,
                                InputRoot!,
                                key.IsDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                                (Key)key.Key,
                                GetAvaloniaRawInputModifiers(key.Modifiers),
                                (PhysicalKey)key.PhysicalKey,
                                key.KeySymbol));
                        }, DispatcherPriority.Input);
                        break;

                    case TextInputEventMessage text:
                        Dispatcher.UIThread.Post(() =>
                        {
                            Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                            Input?.Invoke(new RawTextInputEventArgs(
                                KeyboardDevice,
                                0,
                                InputRoot!,
                                text.Text));
                        }, DispatcherPriority.Input);
                        break;
                }
            }
        }

        private static ProtocolPixelFormat? TryGetValidPixelFormat(ProtocolPixelFormat[]? formats)
        {
            if (formats is not null)
            {
                foreach (var format in formats)
                {
                    if (format is >= 0 and <= ProtocolPixelFormat.MaxValue)
                        return format;
                }
            }

            return null;
        }

        protected virtual Size Measure(Size constraint)
        {
            var l = (Layoutable) InputRoot!;
            l.Measure(constraint);
            return l.DesiredSize;
        }

        public override IEnumerable<object> Surfaces => new[] { this };

        private Framebuffer GetOrCreateFramebuffer()
        {
            lock (_lock)
            {
                if (_format is not { } format)
                    _framebuffer = Framebuffer.Empty;
                else if (_framebuffer.Format != format || _framebuffer.ClientSize != ClientSize || _framebuffer.RenderScaling != RenderScaling)
                    _framebuffer = new Framebuffer(format, ClientSize, RenderScaling);

                return _framebuffer;
            }
        }

        private void SendLastFrameIfNeeded()
        {
            if (IsDisposed)
                return;

            Framebuffer framebuffer;
            long sequenceId;

            lock (_lock)
            {
                if (_lastReceivedFrame != _lastSentFrame || _framebuffer.GetStatus() != FrameStatus.Rendered)
                    return;

                framebuffer = _framebuffer;
                _lastSentFrame = _nextFrameNumber++;
                sequenceId = _lastSentFrame;
            }

            _transport.Send(framebuffer.ToMessage(sequenceId));
        }

        protected void RenderAndSendFrameIfNeeded()
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (_lastReceivedFrame != _lastSentFrame || _format is null)
                    return;
            }

            var framebuffer = GetOrCreateFramebuffer();

            if (framebuffer.Stride > 0)
                Paint?.Invoke(new Rect(framebuffer.ClientSize));

            SendLastFrameIfNeeded();
        }

        public override IMouseDevice MouseDevice { get; } = new MouseDevice();

        public IKeyboardDevice KeyboardDevice { get; }
        
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() =>
            new FuncFramebufferRenderTarget(() => GetOrCreateFramebuffer().Lock(_sendLastFrameIfNeeded));
    }
}
