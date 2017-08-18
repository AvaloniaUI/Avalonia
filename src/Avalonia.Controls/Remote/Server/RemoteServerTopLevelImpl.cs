using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Embedding.Offscreen;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;
using PixelFormat = Avalonia.Platform.PixelFormat;
using ProtocolPixelFormat = Avalonia.Remote.Protocol.Viewport.PixelFormat;

namespace Avalonia.Controls.Remote.Server
{
    class RemoteServerTopLevelImpl : OffscreenTopLevelImplBase, IFramebufferPlatformSurface
    {
        private readonly IAvaloniaRemoteTransport _transport;
        private LockedFramebuffer _framebuffer;
        private object _lock = new object();
        private long _lastSentFrame;
        private long _lastReceivedFrame = -1;
        private bool _invalidated;
        private ProtocolPixelFormat[] _supportedFormats;

        public RemoteServerTopLevelImpl(IAvaloniaRemoteTransport transport)
        {
            _transport = transport;
            _transport.OnMessage += OnMessage;
        }

        private void OnMessage(object obj)
        {
            lock (_lock)
            {
                var lastFrame = obj as FrameReceivedMessage;
                if (lastFrame != null)
                {
                    lock (_lock)
                    {
                        _lastReceivedFrame = lastFrame.SequenceId;
                    }
                    Dispatcher.UIThread.InvokeAsync(CheckNeedsRender);
                }
                var supportedFormats = obj as ClientSupportedPixelFormatsMessage;
                if (supportedFormats != null)
                    _supportedFormats = supportedFormats.Formats;
            }
        }

        public override IEnumerable<object> Surfaces => new[] { this };
        
        FrameMessage RenderFrame(int width, int height, Size dpi, ProtocolPixelFormat? format)
        {
            var fmt = format ?? ProtocolPixelFormat.Rgba8888;
            var bpp = fmt == ProtocolPixelFormat.Rgb565 ? 2 : 4;
            var data = new byte[width * height * bpp];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                _framebuffer = new LockedFramebuffer(handle.AddrOfPinnedObject(), width, height, width * bpp, dpi, (PixelFormat)fmt,
                    null);
                Paint?.Invoke(new Rect(0, 0, width, height));
            }
            finally
            {
                _framebuffer = null;
                handle.Free();
            }
            return new FrameMessage();
        }

        public ILockedFramebuffer Lock()
        {
            if (_framebuffer == null)
                throw new InvalidOperationException("Paint was not requested, wait for Paint event");
            return _framebuffer;
        }

        void CheckNeedsRender()
        {
            ProtocolPixelFormat[] formats;
            lock (_lock)
            {
                if (_lastReceivedFrame != _lastSentFrame && !_invalidated)
                    return;
                formats = _supportedFormats;
            }
            
            //var frame = RenderFrame()
        }

        public override void Invalidate(Rect rect)
        {
            _invalidated = true;
            Dispatcher.UIThread.InvokeAsync(CheckNeedsRender);
        }
    }
}
