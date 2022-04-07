using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

public class ScreenSelector : Selector
{
    private readonly Selector? _previous;

    public ScreenSelector(Selector? previous)
    {
        _previous = previous;
    }
    public override bool InTemplate => _previous?.InTemplate ?? false;

    public override bool IsCombinator => false;

    public override Type? TargetType => _previous?.TargetType;
        
    protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
    {
        if (!(control is ITopLevelScreenSizeProvider logical))
        {
            return SelectorMatch.NeverThisType;
        }

        if (subscribe)
        {
            return new SelectorMatch(new ScreenActivator(logical));
        }

        if (logical.GetScreenSizeProvider() is { } screenSizeProvider)
        {
            return Evaluate(screenSizeProvider);
        }
            
        return SelectorMatch.NeverThisInstance;
    }

    internal static SelectorMatch Evaluate(IScreenSizeProvider screenSizeProvider)
    {
        var match = screenSizeProvider.GetScreenWidth() > 600 && screenSizeProvider.GetScreenHeight() > 600;

        return match ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
    }

    protected override Selector? MovePrevious() => _previous;

    public override string ToString() => "screen";
}
