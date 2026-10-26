using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;

namespace Avalonia.Win32;

/// <summary>
/// Adapts shell virtual-file clipboard formats to <see cref="IStorageFile"/> instances.
/// </summary>
/// <remarks>
/// Windows Explorer uses FileGroupDescriptorW plus indexed FileContents entries when a file has no
/// filesystem path yet, for example while dragging an entry from a ZIP folder.
/// </remarks>
internal static class OleVirtualFileData
{
    private static readonly Guid s_iidIStream = MicroComRuntime.GetGuidFor(typeof(Win32Com.IStream));
    private static readonly Guid s_iidIDataObjectAsyncCapability =
        MicroComRuntime.GetGuidFor(typeof(Win32Com.IDataObjectAsyncCapability));

    internal static readonly DataFormat<byte[]> FileGroupDescriptorFormat =
        DataFormat.CreateBytesPlatformFormat("FileGroupDescriptorW");
    internal static readonly DataFormat<byte[]> FileContentsFormat =
        DataFormat.CreateBytesPlatformFormat("FileContents");

    internal static unsafe List<IStorageFile>? TryCreateFiles(Win32Com.IDataObject dataObject)
    {
        var descriptorFormat = OleDataObjectHelper.ToFormatEtc(
            ClipboardFormatRegistry.GetOrAddFormat(FileGroupDescriptorFormat));
        var descriptorMedium = new STGMEDIUM();

        if (dataObject.GetData(&descriptorFormat, &descriptorMedium) != (uint)UnmanagedMethods.HRESULT.S_OK)
            return null;

        List<Descriptor> descriptors;
        try
        {
            if (descriptorMedium.tymed != TYMED.TYMED_HGLOBAL || descriptorMedium.unionmember == IntPtr.Zero)
                return null;

            descriptors = ReadDescriptors(descriptorMedium.unionmember);
        }
        finally
        {
            UnmanagedMethods.ReleaseStgMedium(ref descriptorMedium);
        }

        if (descriptors.Count == 0)
            return null;

        // Keep the source-side data object alive after IDropTarget.Drop returns. Consumers that retain
        // returned virtual files must dispose them once they finish reading; the operation ends then.
        var operation = new Operation(BeginAsyncOperation(dataObject), descriptors.Count);
        var files = new List<IStorageFile>(descriptors.Count);
        var fileContentsFormat = ClipboardFormatRegistry.GetOrAddFormat(FileContentsFormat);

        for (var index = 0; index < descriptors.Count; index++)
        {
            if (TryCreateFile(dataObject, fileContentsFormat, descriptors[index], index, operation) is { } file)
                files.Add(file);
            else
                operation.CompleteFile();
        }

        return files.Count == 0 ? null : files;
    }

    private static unsafe IStorageFile? TryCreateFile(
        Win32Com.IDataObject dataObject,
        ushort fileContentsFormat,
        Descriptor descriptor,
        int index,
        Operation operation)
    {
        try
        {
            var contentFormat = new FORMATETC
            {
                cfFormat = fileContentsFormat,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = index,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_ISTREAM
            };
            var contentMedium = new STGMEDIUM();
            var result = dataObject.GetData(&contentFormat, &contentMedium);
            if (result != (uint)UnmanagedMethods.HRESULT.S_OK)
            {
                // Some IDataObject implementations expose FileContents only as HGLOBAL, so retry
                // with that medium explicitly when the IStream request fails.
                contentFormat.tymed = TYMED.TYMED_HGLOBAL;
                contentMedium = default;
                result = dataObject.GetData(&contentFormat, &contentMedium);
            }

            if (result != (uint)UnmanagedMethods.HRESULT.S_OK)
            {
                throw new IOException(
                    $"The virtual file stream is unavailable (GetData HRESULT 0x{result:X8}, index {index}).");
            }

            try
            {
                if (contentMedium.tymed == TYMED.TYMED_ISTREAM && contentMedium.unionmember != IntPtr.Zero)
                {
                    // FileContents is obtained on the OLE/UI thread, while consumers commonly copy it
                    // on a worker thread. Marshal IStream explicitly to preserve COM apartment affinity.
                    var iid = s_iidIStream;
                    var marshalResult = UnmanagedMethods.CoMarshalInterThreadInterfaceInStream(
                        ref iid, contentMedium.unionmember, out var marshaledStream);
                    if (marshalResult < 0)
                        Marshal.ThrowExceptionForHR(marshalResult);

                    return new VirtualStorageFile(
                        descriptor,
                        MicroComRuntime.CreateProxyFor<Win32Com.IStream>(marshaledStream, true),
                        operation);
                }

                if (contentMedium.tymed == TYMED.TYMED_HGLOBAL && contentMedium.unionmember != IntPtr.Zero)
                {
                    return new VirtualStorageFile(
                        descriptor, OleDataObjectHelper.ReadBytesFromHGlobal(contentMedium.unionmember), operation);
                }

                throw new IOException(
                    $"The virtual file stream is unavailable (unexpected TYMED {contentMedium.tymed}, index {index}).");
            }
            finally
            {
                UnmanagedMethods.ReleaseStgMedium(ref contentMedium);
            }
        }
        catch (Exception exception)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(
                null, "Failed to create virtual file at index {Index}: {Exception}", index, exception);
            return null;
        }
    }

    internal static unsafe List<Descriptor> ReadDescriptors(IntPtr hGlobal)
    {
        var pointer = UnmanagedMethods.GlobalLock(hGlobal);
        if (pointer == IntPtr.Zero)
            return [];

        try
        {
            var availableSize = UnmanagedMethods.GlobalSize(hGlobal).ToInt64();
            if (availableSize < sizeof(uint))
                return [];

            var count = *(uint*)pointer;
            if (count > int.MaxValue ||
                sizeof(uint) + (long)count * sizeof(UnmanagedMethods.FILEDESCRIPTORW) > availableSize)
                return [];

            var descriptors = new List<Descriptor>((int)count);
            var nativeDescriptors =
                (UnmanagedMethods.FILEDESCRIPTORW*)((byte*)pointer + sizeof(uint));

            for (var index = 0; index < (int)count; index++)
            {
                var descriptor = nativeDescriptors[index];
                var nameLength = 0;
                while (nameLength < UnmanagedMethods.FILEDESCRIPTORW.FileNameLength &&
                    descriptor.cFileName[nameLength] != '\0')
                {
                    nameLength++;
                }

                var name = new string(descriptor.cFileName, 0, nameLength);
                ulong? size = null;

                if ((descriptor.dwFlags & UnmanagedMethods.FILEDESCRIPTORW.FD_FILESIZE) != 0)
                    size = ((ulong)descriptor.nFileSizeHigh << 32) | descriptor.nFileSizeLow;

                descriptors.Add(new Descriptor(name, size));
            }

            return descriptors;
        }
        finally
        {
            UnmanagedMethods.GlobalUnlock(hGlobal);
        }
    }

    internal readonly record struct Descriptor(string Name, ulong? Size);

    private static unsafe Win32Com.IStream? BeginAsyncOperation(Win32Com.IDataObject dataObject)
    {
        Win32Com.IDataObjectAsyncCapability? capability = null;
        var operationStarted = false;

        try
        {
            capability = MicroComRuntime.QueryInterface<Win32Com.IDataObjectAsyncCapability>(dataObject);
            capability.SetAsyncMode(1);
            capability.StartOperation(null);
            operationStarted = true;

            // EndOperation can run when a consumer disposes the last file on a worker thread. Marshal
            // the capability now so that call uses a proxy for the completing thread's COM apartment.
            var iid = s_iidIDataObjectAsyncCapability;
            var result = UnmanagedMethods.CoMarshalInterThreadInterfaceInStream(
                ref iid, capability.GetNativeIntPtr(), out var marshaledCapability);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);

            return MicroComRuntime.CreateProxyFor<Win32Com.IStream>(marshaledCapability, true);
        }
        catch (COMException)
        {
            if (operationStarted)
            {
                try
                {
                    capability!.EndOperation(0, null, (int)Win32Com.DropEffect.Copy);
                }
                catch (COMException)
                {
                }
            }

            return null;
        }
        finally
        {
            capability?.Dispose();
        }
    }

    private sealed class Operation(Win32Com.IStream? marshaledCapability, int remainingCount)
    {
        private Win32Com.IStream? _marshaledCapability = marshaledCapability;
        private int _remainingCount = remainingCount;

        public unsafe void CompleteFile()
        {
            if (Interlocked.Decrement(ref _remainingCount) != 0)
                return;

            var marshaled = Interlocked.Exchange(ref _marshaledCapability, null);
            if (marshaled is null)
                return;

            try
            {
                var marshaledPointer = MicroComRuntime.GetNativeIntPtr(marshaled, owned: true);
                var iid = s_iidIDataObjectAsyncCapability;
                var result = UnmanagedMethods.CoGetInterfaceAndReleaseStream(
                    marshaledPointer, ref iid, out var capabilityPointer);
                if (result < 0)
                    Marshal.ThrowExceptionForHR(result);

                using var capability = MicroComRuntime.CreateProxyFor<Win32Com.IDataObjectAsyncCapability>(
                    capabilityPointer, true);
                capability.EndOperation(0, null, (int)Win32Com.DropEffect.Copy);
            }
            catch (COMException exception)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(
                    null, $"Failed to end an asynchronous virtual-file operation: {exception.Message}");
            }
            finally
            {
                marshaled.Dispose();
            }
        }
    }

    private sealed class VirtualStorageFile : IStorageFile
    {
        private readonly byte[]? _contents;
        private readonly Operation _operation;
        private Win32Com.IStream? _marshaledStream;
        private int _disposed;

        public VirtualStorageFile(
            Descriptor descriptor, Win32Com.IStream? marshaledStream, Operation operation)
        {
            Name = descriptor.Name;
            Size = descriptor.Size;
            Path = new Uri($"virtual-file:///{Uri.EscapeDataString(descriptor.Name)}");
            _marshaledStream = marshaledStream;
            _operation = operation;
        }

        public VirtualStorageFile(Descriptor descriptor, byte[] contents, Operation operation)
            : this(descriptor, (Win32Com.IStream?)null, operation)
        {
            _contents = contents;
        }

        public string Name { get; }
        public Uri Path { get; }
        public bool CanBookmark => false;
        private ulong? Size { get; }

        public Task<Stream> OpenReadAsync()
        {
            if (_contents is not null)
                return Task.FromResult<Stream>(new MemoryStream(_contents, writable: false));

            var marshaledStream = Interlocked.Exchange(ref _marshaledStream, null);
            if (marshaledStream is null)
                throw new IOException("The virtual file stream has already been opened or disposed.");

            try
            {
                // Keep the marshaling stream proxy alive while CoGetInterfaceAndReleaseStream consumes
                // an additional reference. The proxy then releases its original reference in finally.
                var marshaledPointer = MicroComRuntime.GetNativeIntPtr(marshaledStream, owned: true);
                var iid = s_iidIStream;
                var result = UnmanagedMethods.CoGetInterfaceAndReleaseStream(
                    marshaledPointer, ref iid, out var streamPointer);
                if (result < 0)
                    Marshal.ThrowExceptionForHR(result);

                var stream = MicroComRuntime.CreateProxyFor<Win32Com.IStream>(streamPointer, true);
                return Task.FromResult<Stream>(new ComReadStream(stream));
            }
            finally
            {
                marshaledStream.Dispose();
            }
        }

        public Task<Stream> OpenWriteAsync() => Task.FromException<Stream>(new NotSupportedException());
        public Task<StorageItemProperties> GetBasicPropertiesAsync() =>
            Task.FromResult(new StorageItemProperties(Size));
        public Task<string?> SaveBookmarkAsync() => Task.FromResult<string?>(null);
        public Task<IStorageFolder?> GetParentAsync() => Task.FromResult<IStorageFolder?>(null);
        public Task DeleteAsync() => Task.FromException(new NotSupportedException());
        public Task<IStorageItem?> MoveAsync(IStorageFolder destination) =>
            Task.FromException<IStorageItem?>(new NotSupportedException());

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            Interlocked.Exchange(ref _marshaledStream, null)?.Dispose();

            _operation.CompleteFile();
        }
    }

    private sealed class ComReadStream(Win32Com.IStream stream) : Stream
    {
        private Win32Com.IStream? _stream = stream;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override unsafe long Length
        {
            get
            {
                var stream = _stream;
                ObjectDisposedException.ThrowIf(stream is null, this);

                UnmanagedMethods.STATSTG stat = default;
                var result = stream.Stat(&stat, UnmanagedMethods.STATFLAG_NONAME);
                if (result != (int)UnmanagedMethods.HRESULT.S_OK)
                    Marshal.ThrowExceptionForHR(result);

                return checked((long)stat.cbSize);
            }
        }

        public override long Position
        {
            get => Seek(0, SeekOrigin.Current);
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override unsafe int Read(Span<byte> buffer)
        {
            var stream = _stream;
            ObjectDisposedException.ThrowIf(stream is null, this);

            if (buffer.IsEmpty)
                return 0;

            uint bytesRead = 0;
            fixed (byte* pointer = buffer)
            {
                var result = stream.Read(pointer, (uint)buffer.Length, &bytesRead);
                if (result != (int)UnmanagedMethods.HRESULT.S_OK &&
                    result != (int)UnmanagedMethods.HRESULT.S_FALSE)
                    Marshal.ThrowExceptionForHR(result);
            }

            return checked((int)bytesRead);
        }

        protected override void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _stream, null)?.Dispose();

            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override unsafe long Seek(long offset, SeekOrigin origin)
        {
            var stream = _stream;
            ObjectDisposedException.ThrowIf(stream is null, this);

            var dwOrigin = origin switch
            {
                SeekOrigin.Begin => 0,
                SeekOrigin.Current => 1,
                SeekOrigin.End => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            ulong position = 0;
            var result = stream.Seek(offset, dwOrigin, &position);
            if (result != (int)UnmanagedMethods.HRESULT.S_OK)
                Marshal.ThrowExceptionForHR(result);

            return checked((long)position);
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
