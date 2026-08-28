using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.MicroCom;
using Avalonia.Platform.Storage;
using Avalonia.Win32;
using MicroCom.Runtime;
using Xunit;
using Win32Com = Avalonia.Win32.Win32Com;
using Win32UnmanagedMethods = Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class OleVirtualFileDataTests
{
    [Fact]
    public async Task Exposes_Virtual_File_As_StorageFile()
    {
        var expectedContents = new byte[] { 1, 2, 3, 4 };
        var transferItem = new DataTransferItem();
        transferItem.Set(
            OleVirtualFileData.FileGroupDescriptorFormat,
            CreateFileGroupDescriptorBytes(("archive-entry.txt", (ulong)expectedContents.Length)));
        transferItem.Set(OleVirtualFileData.FileContentsFormat, expectedContents);
        var transfer = new DataTransfer();
        transfer.Add(transferItem);

        using var source = new AsyncVirtualFileDataObject(transfer);
        var sourcePointer = source.GetNativeIntPtr<Win32Com.IDataObject>();
        using var sourceProxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(sourcePointer, false);
        using var target = new OleDataObjectToDataTransferWrapper(sourceProxy);
        var targetTransfer = (IDataTransfer)target;

        Assert.Contains(DataFormat.File, targetTransfer.Formats);
        Assert.Equal(2, targetTransfer.Items.Count);
        var item = targetTransfer.Items[0];
        Assert.DoesNotContain(DataFormat.File, targetTransfer.Items[1].Formats);
        using var file = Assert.IsAssignableFrom<IStorageFile>(item.TryGetRaw(DataFormat.File));

        Assert.Equal("archive-entry.txt", file.Name);
        Assert.Equal((ulong)expectedContents.Length, (await file.GetBasicPropertiesAsync()).Size);
        await using var stream = await file.OpenReadAsync();
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, TestContext.Current.CancellationToken);
        Assert.Equal(expectedContents, output.ToArray());

        await Task.Run(file.Dispose, TestContext.Current.CancellationToken);
        Assert.True(source.AsyncMode);
        Assert.Equal(1, source.StartOperationCount);
        Assert.Equal(1, source.EndOperationCount);
    }

    [Fact]
    public unsafe void Rejects_Truncated_FileGroupDescriptorW()
    {
        var memory = Win32UnmanagedMethods.GlobalAlloc(Win32UnmanagedMethods.GlobalAllocFlags.GHND, sizeof(uint));
        var pointer = Win32UnmanagedMethods.GlobalLock(memory);
        *(uint*)pointer = 1;
        Win32UnmanagedMethods.GlobalUnlock(memory);

        try
        {
            Assert.Empty(OleVirtualFileData.ReadDescriptors(memory));
        }
        finally
        {
            Win32UnmanagedMethods.GlobalFree(memory);
        }
    }

    private static unsafe IntPtr CreateFileGroupDescriptor(params (string Name, ulong Size)[] files)
    {
        var memory = Win32UnmanagedMethods.GlobalAlloc(
            Win32UnmanagedMethods.GlobalAllocFlags.GHND,
            sizeof(uint) + files.Length * sizeof(Win32UnmanagedMethods.FILEDESCRIPTORW));
        var pointer = Win32UnmanagedMethods.GlobalLock(memory);

        *(uint*)pointer = (uint)files.Length;
        var descriptors =
            (Win32UnmanagedMethods.FILEDESCRIPTORW*)((byte*)pointer + sizeof(uint));

        for (var index = 0; index < files.Length; index++)
        {
            var descriptor = &descriptors[index];
            descriptor->dwFlags = Win32UnmanagedMethods.FILEDESCRIPTORW.FD_FILESIZE;
            descriptor->nFileSizeHigh = (uint)(files[index].Size >> 32);
            descriptor->nFileSizeLow = (uint)files[index].Size;

            var characterCount = Math.Min(
                files[index].Name.Length, Win32UnmanagedMethods.FILEDESCRIPTORW.FileNameLength - 1);
            files[index].Name.AsSpan(0, characterCount)
                .CopyTo(new Span<char>(
                    descriptor->cFileName, Win32UnmanagedMethods.FILEDESCRIPTORW.FileNameLength));
        }

        Win32UnmanagedMethods.GlobalUnlock(memory);
        return memory;
    }

    private static byte[] CreateFileGroupDescriptorBytes(params (string Name, ulong Size)[] files)
    {
        var memory = CreateFileGroupDescriptor(files);
        try
        {
            var size = checked((int)Win32UnmanagedMethods.GlobalSize(memory).ToInt64());
            var pointer = Win32UnmanagedMethods.GlobalLock(memory);
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, size);
            Win32UnmanagedMethods.GlobalUnlock(memory);
            return bytes;
        }
        finally
        {
            Win32UnmanagedMethods.GlobalFree(memory);
        }
    }

    private sealed class AsyncVirtualFileDataObject(DataTransfer transfer)
        : DataTransferToOleDataObjectWrapper(transfer), Win32Com.IDataObjectAsyncCapability
    {
        public bool AsyncMode { get; private set; }
        public int StartOperationCount { get; private set; }
        public int EndOperationCount { get; private set; }

        int Win32Com.IDataObjectAsyncCapability.AsyncMode => AsyncMode ? 1 : 0;

        public void SetAsyncMode(int doOperationAsync) => AsyncMode = doOperationAsync != 0;

        public unsafe void StartOperation(void* reserved) => StartOperationCount++;

        public int InOperation() => StartOperationCount > EndOperationCount ? 1 : 0;

        public unsafe void EndOperation(int result, void* reserved, int effects) => EndOperationCount++;
    }
}
