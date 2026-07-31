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
internal static partial class OleVirtualFileData
{
    // FILEGROUPDESCRIPTORW is a UINT count followed by packed FILEDESCRIPTORW structures.
    // These sizes and offsets match the Windows SDK shlobj_core.h definitions.
    private const int FileDescriptorSize = 592;
    private const int FileNameOffset = 72;
    private const int FileNameCharacterCount = 260;
    private const int FileSizeHighOffset = 64;
    private const int FileSizeLowOffset = 68;
    private const uint FileDescriptorHasFileSize = 0x00000040;

    private static readonly Guid s_iidIStream = new("0000000C-0000-0000-C000-000000000046");
    private static readonly Guid s_iidIDataObjectAsyncCapability =
        new("3D8B0590-F691-11D2-8EA9-006097DF5BD4");

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

        // Keep the source-side data object alive after IDropTarget.Drop returns. The operation ends
        // after every returned IStorageFile is disposed, as required by the IStorageItem contract.
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
                    // Some IDataObject implementations reject a combined TYMED mask even though they
                    // provide FileContents as HGLOBAL, so retry with that medium explicitly.
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
                        var marshalResult = CoMarshalInterThreadInterfaceInStream(
                            ref iid, contentMedium.unionmember, out var marshaledStream);
                        if (marshalResult < 0)
                            Marshal.ThrowExceptionForHR(marshalResult);

                        files.Add(new VirtualStorageFile(descriptors[index], marshaledStream, operation));
                    }
                    else if (contentMedium.tymed == TYMED.TYMED_HGLOBAL && contentMedium.unionmember != IntPtr.Zero)
                    {
                        files.Add(new VirtualStorageFile(
                            descriptors[index], ReadHGlobal(contentMedium.unionmember), operation));
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
            if (count > int.MaxValue || sizeof(uint) + (long)count * FileDescriptorSize > availableSize)
                return [];

            var descriptors = new List<Descriptor>((int)count);
            for (var index = 0; index < count; index++)
            {
                var descriptor = pointer + sizeof(uint) + index * FileDescriptorSize;
                var flags = *(uint*)descriptor;
                var name = Marshal.PtrToStringUni(descriptor + FileNameOffset, FileNameCharacterCount)?.TrimEnd('\0')
                    ?? string.Empty;
                ulong? size = null;

                if ((flags & FileDescriptorHasFileSize) != 0)
                {
                    var high = *(uint*)(descriptor + FileSizeHighOffset);
                    var low = *(uint*)(descriptor + FileSizeLowOffset);
                    size = ((ulong)high << 32) | low;
                }

                descriptors.Add(new Descriptor(name, size));
            }

            return descriptors;
        }
        finally
        {
            UnmanagedMethods.GlobalUnlock(hGlobal);
        }
    }

    private static byte[] ReadHGlobal(IntPtr hGlobal)
    {
        var pointer = UnmanagedMethods.GlobalLock(hGlobal);
        if (pointer == IntPtr.Zero)
            throw new IOException("The virtual file HGLOBAL could not be locked.");

        try
        {
            var size = checked((int)UnmanagedMethods.GlobalSize(hGlobal).ToInt64());
            var contents = new byte[size];
            Marshal.Copy(pointer, contents, 0, size);
            return contents;
        }
        finally
        {
            UnmanagedMethods.GlobalUnlock(hGlobal);
        }
    }

    internal readonly record struct Descriptor(string Name, ulong? Size);

    private static unsafe IntPtr BeginAsyncOperation(Win32Com.IDataObject dataObject)
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
            var result = CoMarshalInterThreadInterfaceInStream(
                ref iid, capability.GetNativeIntPtr(), out var marshaledCapability);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);

            return marshaledCapability;
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

            return IntPtr.Zero;
        }
        finally
        {
            capability?.Dispose();
        }
    }

    private sealed class Operation(IntPtr marshaledCapability, int remainingCount)
    {
        private readonly int _totalCount = remainingCount;
        private IntPtr _marshaledCapability = marshaledCapability;
        private int _remainingCount = remainingCount;

        public unsafe void Complete()
        {
            if (Interlocked.Decrement(ref _remainingCount) != 0)
                return;

            var marshaled = Interlocked.Exchange(ref _marshaledCapability, IntPtr.Zero);
            if (marshaled == IntPtr.Zero)
                return;

            try
            {
                var iid = s_iidIDataObjectAsyncCapability;
                var result = CoGetInterfaceAndReleaseStream(marshaled, ref iid, out var capabilityPointer);
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
        private IntPtr _marshaledStream;
        private int _disposed;

        public VirtualStorageFile(Descriptor descriptor, IntPtr marshaledStream, Operation operation)
        {
            Name = descriptor.Name;
            Size = descriptor.Size;
            Path = new Uri($"virtual-file:///{Uri.EscapeDataString(descriptor.Name)}");
            _marshaledStream = marshaledStream;
            _operation = operation;
        }

        public VirtualStorageFile(Descriptor descriptor, byte[] contents, Operation operation)
            : this(descriptor, IntPtr.Zero, operation)
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

            var marshaledStream = Interlocked.Exchange(ref _marshaledStream, IntPtr.Zero);
            if (marshaledStream == IntPtr.Zero)
                throw new IOException("The virtual file stream has already been opened or disposed.");

            var iid = s_iidIStream;
            var result = CoGetInterfaceAndReleaseStream(marshaledStream, ref iid, out var stream);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);

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

            var marshaledStream = Interlocked.Exchange(ref _marshaledStream, IntPtr.Zero);
            if (marshaledStream != IntPtr.Zero)
                Marshal.Release(marshaledStream);

            _operation.Complete();
        }
    }

    private sealed class ComReadStream(IntPtr stream) : Stream
    {
        private IntPtr _stream = stream;

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
            ObjectDisposedException.ThrowIf(_stream == IntPtr.Zero, this);

            uint bytesRead = 0;
            fixed (byte* pointer = buffer)
            {
                // IStream::Read is slot 3 after IUnknown. Calling it directly avoids an RCW, which keeps
                // this Windows backend compatible with trimming and NativeAOT.
                var vtable = *(IntPtr**)_stream;
                var read = (delegate* unmanaged[Stdcall]<IntPtr, byte*, uint, uint*, int>)vtable[3];
                var result = read(_stream, pointer, (uint)buffer.Length, &bytesRead);
                if (result < 0)
                    Marshal.ThrowExceptionForHR(result);
            }

            return checked((int)bytesRead);
        }

        protected override void Dispose(bool disposing)
        {
            var stream = Interlocked.Exchange(ref _stream, IntPtr.Zero);
            if (stream != IntPtr.Zero)
                Marshal.Release(stream);

            base.Dispose(disposing);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoMarshalInterThreadInterfaceInStream(
        ref Guid riid, IntPtr unknown, out IntPtr stream);

    [LibraryImport("ole32.dll")]
    private static partial int CoGetInterfaceAndReleaseStream(
        IntPtr stream, ref Guid riid, out IntPtr unknown);
}
