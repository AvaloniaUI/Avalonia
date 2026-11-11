using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Win32;
using Xunit;

namespace Avalonia.IntegrationTests.Win32;

public class DragSourceTests : IDisposable
{
    private Window? _window;

    [Fact]
    public async Task DoDragDropAsync_With_Non_Primary_Pointer_Returns_None_And_Disposes_The_Data()
    {
        _window = new Window { Width = 200, Height = 200 };
        _window.Show();
        await _window.WhenLoadedAsync();

        var dataTransfer = new DisposeCountingDataTransfer();

        var task = new DragSource().DoDragDropAsync(
            CreateTouchPressedEventArgs(_window, isPrimary: false),
            dataTransfer,
            DragDropEffects.Copy);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(DragDropEffects.None, await task);
        Assert.Equal(1, dataTransfer.DisposeCount);
    }

    [Fact]
    public async Task DoDragDropAsync_Disposes_The_Data_When_A_Pending_Drag_Is_Cancelled()
    {
        _window = new Window { Width = 200, Height = 200 };
        _window.Show();
        await _window.WhenLoadedAsync();

        var dataTransfer = new DisposeCountingDataTransfer();

        var task = new DragSource().DoDragDropAsync(
            CreateTouchPressedEventArgs(_window, isPrimary: true),
            dataTransfer,
            DragDropEffects.Copy);

        Assert.False(task.IsCompleted);
        Assert.Equal(0, dataTransfer.DisposeCount);

        _window.Close();

        Assert.Equal(DragDropEffects.None, await task);
        Assert.Equal(1, dataTransfer.DisposeCount);
    }

    private static PointerPressedEventArgs CreateTouchPressedEventArgs(Window window, bool isPrimary)
        => new(
            window,
            new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, isPrimary),
            window,
            default,
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None);

    public void Dispose()
        => _window?.Close();

    private sealed class DisposeCountingDataTransfer : IDataTransfer
    {
        public int DisposeCount { get; private set; }

        public IReadOnlyList<DataFormat> Formats
            => Array.Empty<DataFormat>();

        public IReadOnlyList<IDataTransferItem> Items
            => Array.Empty<IDataTransferItem>();

        public void Dispose()
            => DisposeCount++;
    }
}
