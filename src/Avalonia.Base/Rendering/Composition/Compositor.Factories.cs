using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public partial class Compositor
{
    /// <summary>
    /// Creates a new CompositionTarget
    /// </summary>
    /// <param name="renderTargetFactory">A factory method to create IRenderTarget to be called from the render thread</param>
    /// <returns></returns>
    public CompositionTarget CreateCompositionTarget(Func<IRenderTarget> renderTargetFactory)
    {
        return new CompositionTarget(this, new ServerCompositionTarget(_server, renderTargetFactory));
    }
    
    public CompositionContainerVisual CreateContainerVisual() => new(this, new ServerCompositionContainerVisual(_server));
        
    public ExpressionAnimation CreateExpressionAnimation() => new ExpressionAnimation(this);

    public ExpressionAnimation CreateExpressionAnimation(string expression) => new ExpressionAnimation(this)
    {
        Expression = expression
    };

    public ImplicitAnimationCollection CreateImplicitAnimationCollection() => new ImplicitAnimationCollection(this);

    public CompositionAnimationGroup CreateAnimationGroup() => new CompositionAnimationGroup(this);
}