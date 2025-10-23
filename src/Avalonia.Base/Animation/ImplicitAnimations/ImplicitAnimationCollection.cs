using System;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public class ImplicitAnimationCollection : AvaloniaList<ImplicitAnimation>
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
                    if (oldItem is ImplicitAnimation implicitAnimation)
                    {
                        implicitAnimation.AnimationInvalidated -= InvalidateAnimations;
                    }
                }
            }

            if (e.NewItems is { } newItems)
            {
                foreach (var newItem in newItems)
                {
                    if (newItem is ImplicitAnimation implicitAnimation)
                    {
                        implicitAnimation.AnimationInvalidated += InvalidateAnimations;
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
                if(animation.Property is null)
                    continue;
                
                var group = animationCollection.TryGetValue(animation.Property, out var value) ?
                    value :
                    compositor.CreateAnimationGroup();

                if (group is CompositionAnimationGroup animationGroup && animation.GetCompositionAnimationInternal(visual) is
                        { } newAnimation)
                {
                    animationGroup.Add(newAnimation);
                }

                animationCollection[animation.Property] = group;
            }

            return animationCollection;
        }
    }
}
