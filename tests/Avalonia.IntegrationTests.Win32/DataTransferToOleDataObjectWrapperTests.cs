using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Win32;
using MicroCom.Runtime;
using Xunit;
using FORMATETC = Avalonia.Win32.Interop.UnmanagedMethods.FORMATETC;
using STGMEDIUM = Avalonia.Win32.Interop.UnmanagedMethods.STGMEDIUM;
using Win32Com = Avalonia.Win32.Win32Com;
using Win32UnmanagedMethods = Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class DataTransferToOleDataObjectWrapperTests
{
    private const uint S_OK = (uint)Win32UnmanagedMethods.HRESULT.S_OK;
    private const uint DV_E_TYMED = Win32UnmanagedMethods.DV_E_TYMED;

    // The mask used by WPF and WinForms to check whether a format is available, e.g. on DragEnter.
    private const TYMED WpfAndWinFormsTymeds = TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM | TYMED.TYMED_GDI;

    [Theory]
    [InlineData(TYMED.TYMED_HGLOBAL)]
    [InlineData(WpfAndWinFormsTymeds)]
    [InlineData(TYMED.TYMED_HGLOBAL | TYMED.TYMED_FILE)]
    public unsafe void Text_Is_Available_When_Tymed_Includes_HGlobal(TYMED tymed)
    {
        using var source = CreateSource(DataFormat.Text, "hello");
        using var dataObject = CreateProxy(source);
        var format = CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, tymed);

        Assert.Equal(S_OK, dataObject.QueryGetData(&format));

        var medium = new STGMEDIUM();
        Assert.Equal(S_OK, dataObject.GetData(&format, &medium));

        try
        {
            Assert.Equal(TYMED.TYMED_HGLOBAL, medium.tymed);
            Assert.Equal("hello", ReadString(medium.unionmember));
        }
        finally
        {
            Win32UnmanagedMethods.ReleaseStgMedium(ref medium);
        }
    }

    [Theory]
    [InlineData(TYMED.TYMED_NULL)]
    [InlineData(TYMED.TYMED_ISTREAM)]
    [InlineData(TYMED.TYMED_GDI)]
    [InlineData(TYMED.TYMED_ISTREAM | TYMED.TYMED_FILE)]
    public unsafe void Text_Is_Not_Available_When_Tymed_Excludes_HGlobal(TYMED tymed)
    {
        using var source = CreateSource(DataFormat.Text, "hello");
        using var dataObject = CreateProxy(source);
        var format = CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, tymed);

        Assert.Equal(DV_E_TYMED, dataObject.QueryGetData(&format));

        var medium = new STGMEDIUM();
        Assert.Equal(DV_E_TYMED, dataObject.GetData(&format, &medium));
    }

    [Theory]
    [InlineData(TYMED.TYMED_GDI)]
    [InlineData(WpfAndWinFormsTymeds)]
    [InlineData(TYMED.TYMED_GDI | TYMED.TYMED_ISTREAM)]
    public unsafe void HBitmap_Is_Available_As_Gdi_When_Tymed_Includes_Gdi(TYMED tymed)
    {
        using var bitmap = new WriteableBitmap(new PixelSize(3, 2), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var source = CreateSource(DataFormat.Bitmap, bitmap);
        using var dataObject = CreateProxy(source);
        var format = CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat.CF_BITMAP, tymed);

        Assert.Equal(S_OK, dataObject.QueryGetData(&format));

        var medium = new STGMEDIUM();
        Assert.Equal(S_OK, dataObject.GetData(&format, &medium));

        try
        {
            Assert.Equal(TYMED.TYMED_GDI, medium.tymed);

            var gdiBitmap = new Win32UnmanagedMethods.BITMAP();
            Assert.NotEqual(0, Win32UnmanagedMethods.GetObject(medium.unionmember, sizeof(Win32UnmanagedMethods.BITMAP), (IntPtr)(&gdiBitmap)));
            Assert.Equal(3, gdiBitmap.bmWidth);
            Assert.Equal(2, gdiBitmap.bmHeight);
        }
        finally
        {
            Win32UnmanagedMethods.ReleaseStgMedium(ref medium);
        }
    }

    [Fact]
    public unsafe void Dib_Is_Available_As_HGlobal_When_Tymed_Includes_HGlobal()
    {
        using var bitmap = new WriteableBitmap(new PixelSize(3, 2), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var source = CreateSource(DataFormat.Bitmap, bitmap);
        using var dataObject = CreateProxy(source);
        var format = CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat.CF_DIB, WpfAndWinFormsTymeds);

        Assert.Equal(S_OK, dataObject.QueryGetData(&format));

        var medium = new STGMEDIUM();
        Assert.Equal(S_OK, dataObject.GetData(&format, &medium));

        try
        {
            Assert.Equal(TYMED.TYMED_HGLOBAL, medium.tymed);
            Assert.NotEqual(IntPtr.Zero, medium.unionmember);
        }
        finally
        {
            Win32UnmanagedMethods.ReleaseStgMedium(ref medium);
        }
    }

    [Theory]
    [InlineData(TYMED.TYMED_HGLOBAL, S_OK)]
    [InlineData(WpfAndWinFormsTymeds, DV_E_TYMED)]
    [InlineData(TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM, DV_E_TYMED)]
    public unsafe void GetDataHere_Requires_A_Single_HGlobal_Medium(TYMED tymed, uint expectedResult)
    {
        using var source = CreateSource(DataFormat.Text, "hello");
        using var dataObject = CreateProxy(source);
        var format = CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, tymed);

        var hGlobal = Win32UnmanagedMethods.GlobalAlloc(Win32UnmanagedMethods.GlobalAllocFlags.GHND, 64);
        var medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = hGlobal };

        try
        {
            Assert.Equal(expectedResult, dataObject.GetDataHere(&format, &medium));

            if (expectedResult == S_OK)
                Assert.Equal("hello", ReadString(hGlobal));
        }
        finally
        {
            Win32UnmanagedMethods.GlobalFree(hGlobal);
        }
    }

    private static DataTransferToOleDataObjectWrapper CreateSource<T>(DataFormat<T> dataFormat, T value)
        where T : class
    {
        var item = new DataTransferItem();
        item.Set(dataFormat, value);
        var transfer = new DataTransfer();
        transfer.Add(item);
        return new DataTransferToOleDataObjectWrapper(transfer);
    }

    private static Win32Com.IDataObject CreateProxy(DataTransferToOleDataObjectWrapper source)
        => MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(source.GetNativeIntPtr<Win32Com.IDataObject>(), false);

    private static FORMATETC CreateFormatEtc(Win32UnmanagedMethods.ClipboardFormat clipboardFormat, TYMED tymed)
        => new()
        {
            cfFormat = (ushort)clipboardFormat,
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = tymed
        };

    private static string? ReadString(IntPtr hGlobal)
    {
        var pointer = Win32UnmanagedMethods.GlobalLock(hGlobal);

        try
        {
            return Marshal.PtrToStringUni(pointer);
        }
        finally
        {
            Win32UnmanagedMethods.GlobalUnlock(hGlobal);
        }
    }
}
