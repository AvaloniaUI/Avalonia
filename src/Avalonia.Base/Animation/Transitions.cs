using System;
using Avalonia.Collections;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// A collection of <see cref="ITransition"/> definitions.
    /// </summary>
    public sealed class Transitions : AvaloniaList<ITransition>, IAvaloniaListItemValidator<ITransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transitions"/> class.
        /// </summary>
        public Transitions()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validator = this;
        }

        void IAvaloniaListItemValidator<ITransition>.Validate(ITransition item)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (item is IPropertyTransition { Property: { IsDirect: true } property })
            {
                var display = item is TransitionBase transition ? transition.DebugDisplay : item.ToString();
                throw new InvalidOperationException($"Cannot animate direct property {property} on {display}.");
            }
        }

        internal ImplicitAnimationCollection? GetImplicitAnimations(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animationCollection = compositor.CreateImplicitAnimationCollection();

            foreach (var transition in this)
            {
                if (transition is ICompositionTransition compositionTransition)
                {
                    ICompositionAnimationBase? group = null;

                    if (compositionTransition.GetCompositionAnimation(visual) is { Target: { } target } newAnimation)
                    {
                        group = animationCollection.TryGetValue(target, out var value) ?
                            value :
                            compositor.CreateAnimationGroup();

                        if (group is CompositionAnimationGroup animationGroup)
                            animationGroup.Add(newAnimation);

                        if (group != null)
                            animationCollection[target] = group;
                    }
                }
            }

            return animationCollection.Count == 0 ? null : animationCollection;
        }
    }
}
