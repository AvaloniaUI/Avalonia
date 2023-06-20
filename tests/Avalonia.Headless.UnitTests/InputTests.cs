using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class InputTests
#if XUNIT
    : IDisposable
#endif
{
    private Window _window;
    private Application _setupApp;

#if NUNIT
    [SetUp]
    public void SetUp()
#elif XUNIT
    public InputTests()
#endif
    {
        _setupApp = Application.Current;
        Dispatcher.UIThread.VerifyAccess();
        _window = new Window
        {
            Width = 100,
            Height = 100
        };
    }
    
#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public void Should_Click_Button_On_Window()
    {
        Assert.True(_setupApp == Application.Current);
        
        var buttonClicked = false;
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        button.Click += (_, _) => buttonClicked = true;
        
        _window.Content = button;
        _window.Show();

        _window.MouseDown(new Point(50, 50), MouseButton.Left);
        _window.MouseUp(new Point(50, 50), MouseButton.Left);

        Assert.True(buttonClicked);
    }

#if NUNIT
    [TearDown]
    public void TearDown()
#elif XUNIT
    public void Dispose()
#endif
    {
        Assert.True(_setupApp == Application.Current);

        Dispatcher.UIThread.VerifyAccess();
        _window.Close();
    }
}
