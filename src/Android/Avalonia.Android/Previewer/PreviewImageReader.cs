using System;
using System.Threading;
using Avalonia.Remote.Protocol.Viewport;

namespace Avalonia.Android.Previewer
{
    internal class PreviewImageReader : IDisposable
    {
        private PreviewSurface _surface;
        private readonly PreviewerConnection _connection;
        private long _id = 0;

        public PreviewImageReader(PreviewSurface previewSurface, PreviewerConnection connection)
        {
            _surface = previewSurface;
            _connection = connection;
            _surface.PreviewBitmapReady += Surface_PreviewBitmapReady;
        }

        private void Surface_PreviewBitmapReady(object? sender, PreviewBitmapReadyEventArgs e)
        {
            _id = Interlocked.Increment(ref _id);
            var message = new FrameMessage()
            {
                Data = e.Data,
                Width = e.Width,
                Height = e.Height,
                Format = PixelFormat.Bgra8888,
                Stride = e.RowStride,
                DpiX = 96,
                DpiY = 96,
                SequenceId = _id,
            };

            _connection.Send(message);
        }

        void IDisposable.Dispose()
        {
            _surface.PreviewBitmapReady -= Surface_PreviewBitmapReady;
        }
    }
}
