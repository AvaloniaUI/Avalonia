using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Embedding.Offscreen;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;
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
            }
        }

        protected void SetDpi(Vector dpi)
        {
            _dpi = dpi;
            RenderIfNeeded();
        }

        protected virtual Size Measure(Size constaint)
        {
            var l = (ILayoutable) InputRoot;
            l.Measure(constaint);
            return l.DesiredSize;
        }

        public override IEnumerable<object> Surfaces => new[] { this };
        
        FrameMessage RenderFrame(int width, int height, ProtocolPixelFormat? format)
        {
            var fmt = format ?? ProtocolPixelFormat.Rgba8888;
            var bpp = fmt == ProtocolPixelFormat.Rgb565 ? 2 : 4;
            var data = new byte[width * height * bpp];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                _framebuffer = new LockedFramebuffer(handle.AddrOfPinnedObject(), width, height, width * bpp, _dpi, (PixelFormat)fmt,
                    null);
                Paint?.Invoke(new Rect(0, 0, width, height));
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
            if (ClientSize.Width < 1 || ClientSize.Height < 1)
                return;
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
            _invalidated = true;
            Dispatcher.UIThread.Post(RenderIfNeeded);
        }

        public override IMouseDevice MouseDevice { get; } = new MouseDevice();
    }
}
