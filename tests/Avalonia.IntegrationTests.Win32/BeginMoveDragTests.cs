using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Win32;

public class BeginMoveDragTests : IDisposable
{
    private Window? _window;

    [Fact]
    public async Task BeginMoveDrag_With_Non_Primary_Pointer_Throws_Synchronously()
    {
        _window = new Window { Width = 200, Height = 200 };
        _window.Show();
        await _window.WhenLoadedAsync();

        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, false);
        var e = new PointerPressedEventArgs(
            _window,
            pointer,
            _window,
            default,
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None);
        Exception? postedException = null;

        void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs args)
        {
            postedException = args.Exception;
            args.Handled = true;
        }

        Dispatcher.UIThread.UnhandledException += OnUnhandledException;
        Exception? thrownException;

        try
        {
            thrownException = Record.Exception(() => _window.BeginMoveDrag(e));
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background, TestContext.Current.CancellationToken);
        }
        finally
        {
            Dispatcher.UIThread.UnhandledException -= OnUnhandledException;
        }

        Assert.Null(postedException);
        Assert.IsType<InvalidOperationException>(thrownException);
    }

    public void Dispose()
        => _window?.Close();
}
