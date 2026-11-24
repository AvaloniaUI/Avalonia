using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Win32;
using MicroCom.Runtime;
using Xunit;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;
using Win32Com = Avalonia.Win32.Win32Com;
using Win32UnmanagedMethods = Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class OleDataObjectToDataTransferWrapperTests
{
    private const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;

    [Fact]
    public void Text_Is_Read_When_Clipboard_Is_Briefly_Held_Open()
    {
        using var source = new FailingDataObject("hello", CLIPBRD_E_CANT_OPEN, failureCount: 3);
        using var target = CreateTarget(source);

        Assert.Equal("hello", target.TryGetValue(DataFormat.Text));
        Assert.Equal(4, source.GetDataCount);
    }

    [Fact]
    public void Throws_When_Clipboard_Stays_Held_Open()
    {
        using var source = new FailingDataObject("hello", CLIPBRD_E_CANT_OPEN, failureCount: int.MaxValue);
        using var target = CreateTarget(source);

        var exception = Assert.Throws<COMException>(() => target.TryGetValue(DataFormat.Text));
        Assert.Equal(unchecked((int)CLIPBRD_E_CANT_OPEN), exception.HResult);
    }

    [Fact]
    public void Returns_Null_Without_Retrying_When_GetData_Fails_For_Another_Reason()
    {
        using var source = new FailingDataObject("hello", Win32UnmanagedMethods.DV_E_FORMATETC, failureCount: 1);
        using var target = CreateTarget(source);

        Assert.Null(target.TryGetValue(DataFormat.Text));
        Assert.Equal(1, source.GetDataCount);
    }

    private static IDataTransfer CreateTarget(DataTransferToOleDataObjectWrapper source)
    {
        using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(
            source.GetNativeIntPtr<Win32Com.IDataObject>(), false);
        return new OleDataObjectToDataTransferWrapper(proxy);
    }

    private static DataTransfer CreateTransfer(string text)
    {
        var item = new DataTransferItem();
        item.Set(DataFormat.Text, text);
        var transfer = new DataTransfer();
        transfer.Add(item);
        return transfer;
    }

    // Fails GetData with the given HRESULT a number of times before returning the text.
    private sealed class FailingDataObject(string text, uint failure, int failureCount)
        : DataTransferToOleDataObjectWrapper(CreateTransfer(text)), Win32Com.IDataObject
    {
        private int _remainingFailures = failureCount;

        public int GetDataCount { get; private set; }

        unsafe uint Win32Com.IDataObject.GetData(FORMATETC* format, STGMEDIUM* medium)
        {
            GetDataCount++;

            if (_remainingFailures > 0)
            {
                _remainingFailures--;
                return failure;
            }

            *medium = default;
            medium->tymed = TYMED.TYMED_HGLOBAL;
            return OleDataObjectHelper.WriteDataToHGlobal(DataTransfer!, DataFormat.Text, ref medium->unionmember);
        }
    }
}
