using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;

namespace Avalonia.Rendering.Composition;

public partial class Compositor
{
    /// <summary>
    /// Creates a new CompositionTarget
    /// </summary>
    /// <param name="surfaces">A factory method to create IRenderTarget to be called from the render thread</param>
    /// <returns></returns>
    internal CompositionTarget CreateCompositionTarget(Func<IEnumerable<IPlatformRenderSurface>> surfaces)
    {
        return new CompositionTarget(this, new ServerCompositionTarget(_server, surfaces));
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

    /// <summary>
    /// Creates a compositor attached to the same server-side compositor as this one, but bound to a
    /// different dispatcher and driven by the returned <see cref="CompositorController"/> instead of
    /// the framework's scheduler
    /// </summary>
    /// <param name="dispatcher">
    /// The dispatcher the new compositor is bound to, the dispatcher of the current thread by default
    /// </param>
    public CompositorController CreateCompositorController(Dispatcher? dispatcher = null) =>
        new(this, dispatcher ?? Dispatcher.CurrentDispatcher);
}
