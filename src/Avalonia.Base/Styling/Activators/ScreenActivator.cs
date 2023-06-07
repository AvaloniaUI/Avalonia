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

    protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinWidthMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
}

internal sealed class MaxWidthActivator : MediaQueryActivatorBase
{
    private readonly double _argument;
    
    public MaxWidthActivator(ITopLevelScreenSizeProvider provider, double argument) : base(provider)
    {
        _argument = argument;
    }

    protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxWidthMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
}

internal sealed class MinHeightActivator : MediaQueryActivatorBase
{
    private readonly double _argument;
    
    public MinHeightActivator(ITopLevelScreenSizeProvider provider, double argument) : base(provider)
    {
        _argument = argument;
    }
    
    protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinHeightMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
}

internal sealed class MaxHeightActivator : MediaQueryActivatorBase
{
    private readonly double _argument;
    
    public MaxHeightActivator(ITopLevelScreenSizeProvider provider, double argument) : base(provider)
    {
        _argument = argument;
    }

    protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxHeightMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
}

internal abstract class MediaQueryActivatorBase : StyleActivatorBase, IStyleActivatorSink
{
    private readonly ITopLevelScreenSizeProvider _provider;
    private IScreenSizeProvider? _currentScreenSizeProvider;

    public MediaQueryActivatorBase(
        ITopLevelScreenSizeProvider provider)
    {
        _provider = provider;
    }

    protected IScreenSizeProvider? CurrentMediaInfoProvider => _currentScreenSizeProvider;

    void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

    protected override void Initialize()
    {
        InitialiseScreenSizeProvider();

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

        ReevaluateIsActive();
    }

    private void ScreenSizeChanged(object? sender, EventArgs e)
    {
        ReevaluateIsActive();
    }
}
