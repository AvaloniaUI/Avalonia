using System;
using System.Collections.Generic;
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

internal class MultiSceneApplicationLifetime : IMultiSceneApplicationLifetime
{
    private List<Controls.Scene> _scenes = new List<Controls.Scene>();
    public IReadOnlyList<Controls.Scene> Scenes => _scenes;

    public void AddScene(Controls.Scene scene)
    {
        if(_scenes.Contains(scene))
        {
            throw new InvalidOperationException($"Scene of type {scene.GetType()} has already been added to this lifetime");
        }

        _scenes.Add(scene);
    }

    public void RemoveScene(Controls.Scene scene)
    {
        _scenes.Remove(scene);
    }
}
