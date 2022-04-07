using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling.Activators;

internal sealed class MinWidthActivator : MediaQueryActivatorBase
{
    private readonly double _argument;
    
    public MinWidthActivator(ITopLevelScreenSizeProvider provider, double argument) : base(provider)
    {
        _argument = argument;
    }
    
    protected override bool IsMatching() => CurrentMediaInfoProvider != null && MinWidthMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
}

internal abstract class MediaQueryActivatorBase : StyleActivatorBase
{
    private readonly ITopLevelScreenSizeProvider _provider;
    private IScreenSizeProvider? _currentScreenSizeProvider;

    public MediaQueryActivatorBase(
        ITopLevelScreenSizeProvider provider)
    {
        _provider = provider;
    }

    protected IScreenSizeProvider? CurrentMediaInfoProvider => _currentScreenSizeProvider;

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

    protected abstract bool IsMatching();
}
