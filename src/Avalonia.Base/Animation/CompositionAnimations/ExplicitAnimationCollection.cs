using System;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public class ExplicitAnimationCollection : AvaloniaList<CompositionAnimation>
    {
        internal event EventHandler? Invalidated;

        private Visual? _visual;

        public ExplicitAnimationCollection()
        {
            this.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is { } oldItems)
            {
                foreach (var oldItem in oldItems)
                {
                    if (oldItem is CompositionAnimation explicitAnimation)
                    {
                        explicitAnimation.Detach();
                        explicitAnimation.AnimationInvalidated -= ResetAnimation;
                    }
                }
            }

            if (e.NewItems is { } newItems)
            {
                foreach (var newItem in newItems)
                {
                    if (newItem is CompositionAnimation explicitAnimation)
                    {
                        explicitAnimation.AnimationInvalidated += ResetAnimation;
                    }
                }
            }

            OnAnimationInvalidated();
        }

        private void ResetAnimation(object? sender, EventArgs e)
        {
            if(sender is CompositionAnimation animation && _visual is { } visual)
            {
                animation.Detach();

                AttachAnimation(visual, animation);
            }
        }

        private void OnAnimationInvalidated()
        {
            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        internal void Detach(Visual visual)
        {
            foreach (var animation in this)
            {
                animation.Detach();
            }
        }

        internal void Attach(Visual visual)
        {
            _visual = visual;

            var compositionVisual = ElementComposition.GetElementVisual(visual);

            if (compositionVisual == null)
                return;

            Detach(visual);

            foreach (var animation in this)
            {
                AttachAnimation(visual, animation);
            }
        }

        private static void AttachAnimation(Visual visual, CompositionAnimation animation)
        {
            if (animation.GetCompositionAnimationInternal(visual) is KeyFrameAnimation newAnimation
                                && newAnimation.Target is { } target)
            {
                animation.Attach(visual, newAnimation);
            }
        }
    }
}
