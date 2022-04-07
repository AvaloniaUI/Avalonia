using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling.Activators;

internal sealed class ScreenActivator : StyleActivatorBase
{
    private readonly ITopLevelScreenSizeProvider _provider;
    private IScreenSizeProvider? _currentScreenSizeProvider;

    public ScreenActivator(
        ITopLevelScreenSizeProvider provider)
    {
        _provider = provider;
    }

    protected override void Initialize()
    {
        InitialiseScreenSizeProvider();
        PublishNext(IsMatching());
        _provider.ScreenSizeProviderChanged += ScreenSizeProviderChanged;
    }

    protected override void Deinitialize()
    {
        _provider.ScreenSizeProviderChanged -= ScreenSizeProviderChanged;

        if (_currentScreenSizeProvider is { })
        {
            _currentScreenSizeProvider.ScreenSizeChanged -= ScreenSizeChanged;
            _currentScreenSizeProvider = null;
        }
    }

    private void ScreenSizeProviderChanged(object? sender, EventArgs e)
    {
        if (_currentScreenSizeProvider is { })
        {
            _currentScreenSizeProvider.ScreenSizeChanged -= ScreenSizeChanged;
            _currentScreenSizeProvider = null;
        }
            
        InitialiseScreenSizeProvider();
    }

    private void InitialiseScreenSizeProvider()
    {
        if (_provider.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            _currentScreenSizeProvider = screenSizeProvider;

            _currentScreenSizeProvider.ScreenSizeChanged += ScreenSizeChanged;
        }
            
        PublishNext(IsMatching());
    }

    private void ScreenSizeChanged(object? sender, EventArgs e)
    {
        PublishNext(IsMatching());
    }

    private bool IsMatching() => _currentScreenSizeProvider != null && ScreenSelector.Evaluate(_currentScreenSizeProvider).IsMatch;
}
