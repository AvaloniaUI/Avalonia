using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Remote.Protocol.Viewport;
using PlatformPixelFormat = Avalonia.Platform.PixelFormat;
using ProtocolPixelFormat = Avalonia.Remote.Protocol.Viewport.PixelFormat;

namespace Avalonia.Controls.Remote.Server
{
    internal partial class RemoteServerTopLevelImpl
    {
        private enum FrameStatus
        {
            NotRendered,
            Rendered,
            CopiedToMessage
        }

        private sealed class Framebuffer
        {
            public static Framebuffer Empty { get; } =
                new(ProtocolPixelFormat.Rgba8888, default, new Vector(96.0, 96.0));

            private readonly object _dataLock = new();
            private readonly byte[] _data; // for rendering only
            private readonly byte[] _dataCopy; // for messages only
            private FrameStatus _status = FrameStatus.NotRendered;

            public Framebuffer(ProtocolPixelFormat format, Size clientSize, Vector dpi)
            {
                var frameSize = PixelSize.FromSizeWithDpi(clientSize, dpi);
                if (frameSize.Width <= 0 || frameSize.Height <= 0)
                    frameSize = PixelSize.Empty;

                var bpp = format == ProtocolPixelFormat.Rgb565 ? 2 : 4;
                var stride = frameSize.Width * bpp;
                var dataLength = Math.Max(0, stride * frameSize.Height);

                Format = format;
                FrameSize = frameSize;
                ClientSize = clientSize;
                Dpi = dpi;

                (Stride, _data, _dataCopy) = dataLength > 0 ?
                    (stride, new byte[dataLength], new byte[dataLength]) :
                    (0, Array.Empty<byte>(), Array.Empty<byte>());
            }

            public ProtocolPixelFormat Format { get; }

            public Size ClientSize { get; }

            public Vector Dpi { get; }

            public PixelSize FrameSize { get; }

            public int Stride { get; }

            public FrameStatus GetStatus()
            {
                lock (_dataLock)
                    return _status;
            }

            public ILockedFramebuffer Lock(Action onUnlocked)
            {
                var handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                Monitor.Enter(_dataLock);

                try
                {
                    return new LockedFramebuffer(
                        handle.AddrOfPinnedObject(),
                        FrameSize,
                        Stride,
                        Dpi,
                        new PlatformPixelFormat((PixelFormatEnum)Format),
                        () =>
                        {
                            handle.Free();
                            Array.Copy(_data, _dataCopy, _data.Length);
                            _status = FrameStatus.Rendered;
                            Monitor.Exit(_dataLock);
                            onUnlocked();
                        });
                }
                catch
                {
                    handle.Free();
                    Monitor.Exit(_dataLock);
                    throw;
                }
            }

            /// <remarks>The returned message must be kept around, as it contains a shared buffer.</remarks>
            public FrameMessage ToMessage(long sequenceId)
            {
                lock (_dataLock)
                    _status = FrameStatus.CopiedToMessage;

                return new FrameMessage
                {
                    SequenceId = sequenceId,
                    Data = _dataCopy,
                    Format = Format,
                    Width = FrameSize.Width,
                    Height = FrameSize.Height,
                    Stride = Stride,
                    DpiX = Dpi.X,
                    DpiY = Dpi.Y
                };
            }
        }
    }
}
