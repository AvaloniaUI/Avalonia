using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public partial class Compositor
{
    /// <summary>
    /// Creates a new CompositionTarget
    /// </summary>
    /// <param name="surfaces">A factory method to create IRenderTarget to be called from the render thread</param>
    /// <returns></returns>
    internal CompositionTarget CreateCompositionTarget(Func<IEnumerable<object>> surfaces)
    {
        return new CompositionTarget(this, new ServerCompositionTarget(_server, surfaces, DiagnosticTextRenderer));
    }
    
    public CompositionContainerVisual CreateContainerVisual() => new(this, new ServerCompositionContainerVisual(_server));
        
    public ExpressionAnimation CreateExpressionAnimation() => new ExpressionAnimation(this);

    public ExpressionAnimation CreateExpressionAnimation(string expression) => new ExpressionAnimation(this)
    {
        Expression = expression
    };

    public ImplicitAnimationCollection CreateImplicitAnimationCollection() => new ImplicitAnimationCollection(this);

    public CompositionAnimationGroup CreateAnimationGroup() => new CompositionAnimationGroup(this);

    public CompositionSolidColorVisual CreateSolidColorVisual() =>
        new(this, new ServerCompositionSolidColorVisual(Server));

    public CompositionCustomVisual CreateCustomVisual(CompositionCustomVisualHandler handler) => new(this, handler);

    public CompositionSurfaceVisual CreateSurfaceVisual() => new(this, new ServerCompositionSurfaceVisual(_server));

    public CompositionDrawingSurface CreateDrawingSurface() => new(this);

    public CompositionSolidColorBrush CreateSolidColorBrush() => new(this, new ServerCompositionSolidColorBrush(Server));
    public CompositionSolidColorBrush CreateSolidColorBrush(Color color) => new(this, new ServerCompositionSolidColorBrush(Server), color);
    public CompositionLinearGradientBrush CreateLinearGradientBrush() => new(this, new ServerCompositionLinearGradientBrush(Server));
    public CompositionConicGradientBrush CreateConicGradientBrush() => new(this, new ServerCompositionConicGradientBrush(Server));
    public CompositionRadialGradientBrush CreateRadialGradientBrush() => new(this, new ServerCompositionRadialGradientBrush(Server));
    public CompositionGradientStop CreateCompositionGradientStop(double offset, Color color) => new(this, new ServerCompositionGradientStop(Server), offset, color);
    public CompositionGradientStop CreateCompositionGradientStop() => new(this, new ServerCompositionGradientStop(Server));
}
