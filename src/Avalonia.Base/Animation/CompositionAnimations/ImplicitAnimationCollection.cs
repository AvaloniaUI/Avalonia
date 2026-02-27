using System;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public class ImplicitAnimationCollection : AvaloniaList<CompositionAnimation>
    {
        internal event EventHandler? Invalidated;

        public ImplicitAnimationCollection()
        {
            this.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is { } oldItems)
            {
                foreach (var oldItem in oldItems)
                {
                    if (oldItem is CompositionAnimation compositionAnimation)
                    {
                        compositionAnimation.AnimationInvalidated -= InvalidateAnimations;
                    }
                }
            }

            if (e.NewItems is { } newItems)
            {
                foreach (var newItem in newItems)
                {
                    if (newItem is CompositionAnimation compositionAnimation)
                    {
                        compositionAnimation.AnimationInvalidated += InvalidateAnimations;
                    }
                }
            }

            OnAnimationInvalidated();

            void InvalidateAnimations(object? sender, EventArgs e)
            {
                OnAnimationInvalidated();
            }
        }

        private void OnAnimationInvalidated()
        {
            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        internal Rendering.Composition.Animations.ImplicitAnimationCollection? GetAnimations(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animationCollection = compositor.CreateImplicitAnimationCollection();

            foreach (var animation in this)
            {
                ICompositionAnimationBase? group = null;

                if (animation.GetCompositionAnimationInternal(visual) is
                    { } newAnimation && newAnimation.Target is { } target)
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

            return animationCollection;
        }
    }
}
