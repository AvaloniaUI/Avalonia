using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

public sealed class MinWidthMediaSelector : MediaSelector<double>
{
    public MinWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
    {
        if (!(control is ITopLevelScreenSizeProvider logical))
        {
            return SelectorMatch.NeverThisType;
        }

        if (subscribe)
        {
            return new SelectorMatch(new MinWidthActivator(logical, Argument));
        }

        if (logical.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            return Evaluate(screenSizeProvider, Argument);
        }
            
        return SelectorMatch.NeverThisInstance;
    }

    internal static SelectorMatch Evaluate(IScreenSizeProvider screenSizeProvider, double argument)
    {
        return screenSizeProvider.GetScreenWidth() >=  argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
    }
    public override string ToString() => "min-width";
}

public sealed class MaxWidthMediaSelector : MediaSelector<double>
{
    public MaxWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
    {
        if (!(control is ITopLevelScreenSizeProvider logical))
        {
            return SelectorMatch.NeverThisType;
        }

        if (subscribe)
        {
            return new SelectorMatch(new MaxWidthActivator(logical, Argument));
        }

        if (logical.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            return Evaluate(screenSizeProvider, Argument);
        }
            
        return SelectorMatch.NeverThisInstance;
    }

    internal static SelectorMatch Evaluate(IScreenSizeProvider screenSizeProvider, double argument)
    {
        return screenSizeProvider.GetScreenWidth() <=  argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
    }
    public override string ToString() => "max-width";
}

public sealed class MinHeightMediaSelector : MediaSelector<double>
{
    public MinHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
    {
        if (!(control is ITopLevelScreenSizeProvider logical))
        {
            return SelectorMatch.NeverThisType;
        }

        if (subscribe)
        {
            return new SelectorMatch(new MinHeightActivator(logical, Argument));
        }

        if (logical.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            return Evaluate(screenSizeProvider, Argument);
        }
            
        return SelectorMatch.NeverThisInstance;
    }

    internal static SelectorMatch Evaluate(IScreenSizeProvider screenSizeProvider, double argument)
    {
        return screenSizeProvider.GetScreenHeight() >=  argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
    }
    public override string ToString() => "min-height";
}

public sealed class MaxHeightMediaSelector : MediaSelector<double>
{
    public MaxHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
    {
        if (!(control is ITopLevelScreenSizeProvider logical))
        {
            return SelectorMatch.NeverThisType;
        }

        if (subscribe)
        {
            return new SelectorMatch(new MaxHeightActivator(logical, Argument));
        }

        if (logical.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            return Evaluate(screenSizeProvider, Argument);
        }
            
        return SelectorMatch.NeverThisInstance;
    }

    internal static SelectorMatch Evaluate(IScreenSizeProvider screenSizeProvider, double argument)
    {
        return screenSizeProvider.GetScreenHeight() <=  argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
    }
    public override string ToString() => "max-height";
}

public abstract class MediaSelector<T> : Selector
{
    private readonly Selector? _previous;
    private T _argument;

    public MediaSelector(Selector? previous, T argument)
    {
        _previous = previous;
        _argument = argument;
    }

    protected T Argument => _argument;
    
    public override bool InTemplate => _previous?.InTemplate ?? false;

    public override bool IsCombinator => false;

    public override Type? TargetType => _previous?.TargetType;

    protected override Selector? MovePrevious() => _previous;
}
