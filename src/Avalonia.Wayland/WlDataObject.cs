using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal sealed class WlDataObject : IDataObject, IDisposable, WlDataOffer.IEvents
    {
        private readonly AvaloniaWaylandPlatform _platform;

        private const int BufferSize = 1024;

        public WlDataObject(AvaloniaWaylandPlatform platform, WlDataOffer wlDataOffer)
        {
            _platform = platform;
            WlDataOffer = wlDataOffer;
            WlDataOffer.Events = this;
            MimeTypes = new List<string>();
        }

        internal WlDataOffer WlDataOffer { get; }

        internal List<string> MimeTypes { get; }

        internal DragDropEffects OfferedDragDropEffects { get; private set; }

        internal DragDropEffects MatchedDragDropEffects { get; private set; }

        public IEnumerable<string> GetDataFormats()
        {
            if (MimeTypes.Contains(Wayland.MimeTypes.Text) || MimeTypes.Contains(Wayland.MimeTypes.TextUtf8))
                yield return DataFormats.Text;
            if (MimeTypes.Contains(Wayland.MimeTypes.UriList))
                yield return DataFormats.Files;
        }

        public bool Contains(string dataFormat) => dataFormat switch
            {
                nameof(DataFormats.Text) => MimeTypes.Contains(Wayland.MimeTypes.Text) || MimeTypes.Contains(Wayland.MimeTypes.TextUtf8),
                nameof(DataFormats.Files) => MimeTypes.Contains(Wayland.MimeTypes.UriList),
                _ => false
            };

        public string? GetText()
        {
            var mimeType = MimeTypes.FirstOrDefault(static x => x is Wayland.MimeTypes.Text) ?? MimeTypes.FirstOrDefault(static x => x is Wayland.MimeTypes.TextUtf8);
            if (mimeType is null)
                return null;

            var fd = Receive(mimeType);
            var result = fd < 0 ? null : ReceiveText(fd);
            return result;
        }

        public IEnumerable<IStorageItem>? GetFileNames()
        {
            if (!MimeTypes.Contains(Wayland.MimeTypes.UriList))
                return null;

            var fd = Receive(Wayland.MimeTypes.UriList);
            var text = fd < 0 ? null : ReceiveText(fd);
            return text?
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(static x => StorageProviderHelpers.TryCreateBclStorageItem(new Uri(x).LocalPath))
                .Where(static x => x is not null)!;
        }

        public object? Get(string dataFormat) =>
            dataFormat switch
            {
                nameof(DataFormats.Text) => GetText(),
                nameof(DataFormats.Files) => GetFileNames(),
                _ => null
            };

        public void OnOffer(WlDataOffer eventSender, string mimeType) => MimeTypes.Add(mimeType);

        public void OnSourceActions(WlDataOffer eventSender, WlDataDeviceManager.DndActionEnum sourceActions) => OfferedDragDropEffects = (DragDropEffects)sourceActions;

        public void OnAction(WlDataOffer eventSender, WlDataDeviceManager.DndActionEnum dndAction) => MatchedDragDropEffects = (DragDropEffects)dndAction;

        public void Dispose() => WlDataOffer.Dispose();

        private unsafe int Receive(string mimeType)
        {
            var fds = stackalloc int[2];
            if (LibC.pipe2(fds, FileDescriptorFlags.O_RDONLY) < 0)
            {
                WlDataOffer.Dispose();
                return -1;
            }

            WlDataOffer.Receive(mimeType, fds[1]);
            _platform.WlDisplay.Roundtrip();
            LibC.close(fds[1]);
            return fds[0];
        }

        private static unsafe string ReceiveText(int fd)
        {
            var buffer = stackalloc byte[BufferSize];
            var sb = new StringBuilder();
            while (true)
            {
                var read = LibC.read(fd, (IntPtr)buffer, BufferSize);
                if (read <= 0)
                    break;
                sb.Append(Encoding.UTF8.GetString(buffer, read));
            }

            LibC.close(fd);
            return sb.ToString();
        }
    }
}
