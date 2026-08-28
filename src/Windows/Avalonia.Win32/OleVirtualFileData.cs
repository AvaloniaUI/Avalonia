using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
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

        try
        {
            for (var index = 0; index < descriptors.Count; index++)
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

                        files.Add(new VirtualStorageFile(
                            descriptors[index], new MarshaledInterface(marshaledStream), operation));
                    }
                    else if (contentMedium.tymed == TYMED.TYMED_HGLOBAL && contentMedium.unionmember != IntPtr.Zero)
                    {
                        files.Add(new VirtualStorageFile(
                            descriptors[index], OleDataObjectHelper.ReadBytesFromHGlobal(contentMedium.unionmember),
                            operation));
                    }
                    else
                    {
                        throw new IOException(
                            $"The virtual file stream is unavailable (unexpected TYMED {contentMedium.tymed}, index {index}).");
                    }
                }
                finally
                {
                    UnmanagedMethods.ReleaseStgMedium(ref contentMedium);
                }
            }

            return files;
        }
        catch
        {
            foreach (var file in files)
                file.Dispose();

            operation.DisposeRemaining(files.Count);
            throw;
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

    private static unsafe MarshaledInterface? BeginAsyncOperation(Win32Com.IDataObject dataObject)
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

            return new MarshaledInterface(marshaledCapability);
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

    private sealed class Operation(MarshaledInterface? marshaledCapability, int remainingCount)
    {
        private readonly int _totalCount = remainingCount;
        private MarshaledInterface? _marshaledCapability = marshaledCapability;
        private int _remainingCount = remainingCount;

        public unsafe void Complete()
        {
            if (Interlocked.Decrement(ref _remainingCount) != 0)
                return;

            var marshaled = Interlocked.Exchange(ref _marshaledCapability, null)?.Take() ?? IntPtr.Zero;
            if (marshaled == IntPtr.Zero)
                return;

            try
            {
                var iid = s_iidIDataObjectAsyncCapability;
                var result = UnmanagedMethods.CoGetInterfaceAndReleaseStream(
                    marshaled, ref iid, out var capabilityPointer);
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
        }

        public void DisposeRemaining(int completedCount)
        {
            for (var index = completedCount; index < _totalCount; index++)
                Complete();
        }
    }

    private sealed class VirtualStorageFile : IStorageFile
    {
        private readonly byte[]? _contents;
        private readonly Operation _operation;
        private MarshaledInterface? _marshaledStream;
        private int _disposed;

        public VirtualStorageFile(
            Descriptor descriptor, MarshaledInterface? marshaledStream, Operation operation)
        {
            Name = descriptor.Name;
            Size = descriptor.Size;
            Path = new Uri($"virtual-file:///{Uri.EscapeDataString(descriptor.Name)}");
            _marshaledStream = marshaledStream;
            _operation = operation;
        }

        public VirtualStorageFile(Descriptor descriptor, byte[] contents, Operation operation)
            : this(descriptor, (MarshaledInterface?)null, operation)
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

            var marshaledStream = Interlocked.Exchange(ref _marshaledStream, null)?.Take() ?? IntPtr.Zero;
            if (marshaledStream == IntPtr.Zero)
                throw new IOException("The virtual file stream has already been opened or disposed.");

            var iid = s_iidIStream;
            var result = UnmanagedMethods.CoGetInterfaceAndReleaseStream(
                marshaledStream, ref iid, out var streamPointer);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);

            var stream = MicroComRuntime.CreateProxyFor<Win32Com.IStream>(streamPointer, true);
            return Task.FromResult<Stream>(new ComReadStream(stream));
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

            _operation.Complete();
        }
    }

    private sealed class ComReadStream(Win32Com.IStream stream) : Stream
    {
        private Win32Com.IStream? _stream = stream;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override unsafe int Read(Span<byte> buffer)
        {
            var stream = _stream;
            ObjectDisposedException.ThrowIf(stream is null, this);

            if (buffer.IsEmpty)
                return 0;

            fixed (byte* pointer = buffer)
                return checked((int)stream.Read(pointer, (uint)buffer.Length));
        }

        protected override void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _stream, null)?.Dispose();

            base.Dispose(disposing);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class MarshaledInterface(IntPtr stream) : CriticalFinalizerObject, IDisposable
    {
        private IntPtr _stream = stream;

        public IntPtr Take()
        {
            var result = Interlocked.Exchange(ref _stream, IntPtr.Zero);
            GC.SuppressFinalize(this);
            return result;
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        private void Release()
        {
            var result = Interlocked.Exchange(ref _stream, IntPtr.Zero);
            if (result != IntPtr.Zero)
                Marshal.Release(result);
        }

        ~MarshaledInterface()
        {
            try
            {
                Release();
            }
            catch
            {
                // A finalizer must not let a COM cleanup failure terminate the process.
            }
        }
    }
}
