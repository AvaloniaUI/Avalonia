using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

internal sealed class MinWidthMediaSelector : MediaSelector<double>
{
    public MinWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }

    private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
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

    public override string ToString(Style? owner)
    {
        throw new NotImplementedException();
    }
}

public sealed class MaxWidthMediaSelector : MediaSelector<double>
{
    public MaxWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
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

    public override string ToString(Style? owner)
    {
        throw new NotImplementedException();
    }
}

public sealed class MinHeightMediaSelector : MediaSelector<double>
{
    public MinHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
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

    public override string ToString(Style? owner)
    {
        throw new NotImplementedException();
    }
}

public sealed class MaxHeightMediaSelector : MediaSelector<double>
{
    public MaxHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
    {
    }
    
    private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
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

    public override string ToString(Style? owner)
    {
        throw new NotImplementedException();
    }
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
    
    internal override bool InTemplate => _previous?.InTemplate ?? false;

    internal override bool IsCombinator => false;

    internal override Type? TargetType => _previous?.TargetType;

    public override string ToString(Style? owner)
    {
        throw new NotImplementedException();
    }

    private protected override Selector? MovePrevious() => _previous;

    private protected override Selector? MovePreviousOrParent() => _previous;
}
