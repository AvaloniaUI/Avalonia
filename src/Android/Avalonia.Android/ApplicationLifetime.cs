using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;

namespace Avalonia.Android;

internal class ApplicationLifetime : IActivityApplicationLifetime, ISingleViewApplicationLifetime
{
    private Control? _mainView;

    public Func<Control>? MainViewFactory { get; set; }

    public Control? MainView
    {
        get => _mainView; set
        {
            _mainView = value;

            Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)?.Log(this, "ISingleViewApplicationLifetime.MainView is not fully supported on Android." +
                " Consider setting IActivityApplicationLifetime.MainViewFactory.");
            if (_mainView != null)
                MainViewFactory = () => _mainView;
            else
                MainViewFactory = null;
        }
    }
}
