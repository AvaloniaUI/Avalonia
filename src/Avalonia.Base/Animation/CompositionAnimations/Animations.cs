using Avalonia.Collections;
using Avalonia.Rendering.Composition;

namespace Avalonia.Animation
{
    public static class Animations
    {
        public static readonly AttachedProperty<ImplicitAnimationCollection?> ImplicitAnimationsProperty =
            AvaloniaProperty.RegisterAttached<Visual, ImplicitAnimationCollection?>(
                "ImplicitAnimations",
                typeof(Animations));

        public static readonly AttachedProperty<ExplicitAnimationCollection?> ExplicitAnimationsProperty =
            AvaloniaProperty.RegisterAttached<Visual, ExplicitAnimationCollection?>(
                "ExplicitAnimations",
                typeof(Animations));

        public static void SetImplicitAnimations(Visual visual, ImplicitAnimationCollection? value)
        {
            visual?.SetValue(ImplicitAnimationsProperty, value);
        }

        public static void SetExplicitAnimations(Visual visual, ExplicitAnimationCollection? value)
        {
            visual?.SetValue(ExplicitAnimationsProperty, value);
        }

        public static ImplicitAnimationCollection? GetImplicitAnimations(Visual visual) => visual.GetValue(ImplicitAnimationsProperty);

        public static ExplicitAnimationCollection? GetExplicitAnimations(Visual visual) => visual.GetValue(ExplicitAnimationsProperty);

        public static readonly AttachedProperty<bool> EnableAnimationsProperty =
            AvaloniaProperty.RegisterAttached<Visual, bool>(
                "EnableAnimations",
                typeof(Animations), defaultValue: true, inherits: true);

        public static void SetEnableAnimations(Visual visual, bool value)
        {
            visual?.SetValue(EnableAnimationsProperty, value);
        }

        public static bool GetEnableAnimations(Visual visual) => visual.GetValue(EnableAnimationsProperty);

        static Animations()
        {
            ImplicitAnimationsProperty.Changed.AddClassHandler<Visual>(OnAnimationsPropertyChanged);
            ExplicitAnimationsProperty.Changed.AddClassHandler<Visual>(OnAnimationsPropertyChanged);
            EnableAnimationsProperty.Changed.AddClassHandler<Visual>(OnAnimationsPropertyChanged);
        }

        private static void OnAnimationsPropertyChanged(Visual visual, AvaloniaPropertyChangedEventArgs args)
        {
            void AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
            {
                Invalidate(visual);
            }

            if (args.Property == ImplicitAnimationsProperty)
            {
                if (args.OldValue is ImplicitAnimationCollection oldImplicitSet)
                {
                    oldImplicitSet.Invalidated -= (s, e) => UpdateAnimations(s as AvaloniaList<CompositionAnimation>, visual);
                    visual.AttachedToVisualTree -= AttachedToVisualTree;
                }


                if (args.NewValue is ImplicitAnimationCollection newImplicitSet)
                {
                    newImplicitSet.Invalidated += (s, e) => UpdateAnimations(s as AvaloniaList<CompositionAnimation>,  visual);
                    UpdateAnimations(newImplicitSet, visual);
                    visual.AttachedToVisualTree += AttachedToVisualTree;
                }
            }
            else if (args.Property == ExplicitAnimationsProperty)
            {
                if (args.OldValue is ExplicitAnimationCollection oldExplicitSet)
                {
                    oldExplicitSet.Detach(visual);
                    oldExplicitSet.Invalidated -= (s, e) => UpdateAnimations(s as AvaloniaList<CompositionAnimation>, visual);
                    visual.AttachedToVisualTree -= AttachedToVisualTree;
                }


                if (args.NewValue is ExplicitAnimationCollection newExplicitSet)
                {
                    newExplicitSet.Invalidated += (s, e) => UpdateAnimations(s as AvaloniaList<CompositionAnimation>, visual);
                    UpdateAnimations(newExplicitSet, visual);
                    visual.AttachedToVisualTree += AttachedToVisualTree;
                }
            }
            else if (args.Property == EnableAnimationsProperty)
            {
                Invalidate(visual);
            }
        }

        private static void Invalidate(Visual? visual)
        {
            if (visual == null)
                return;

            UpdateAnimations(GetImplicitAnimations(visual), visual);
            var explicitAnimationCollection = GetExplicitAnimations(visual);
            explicitAnimationCollection?.Detach(visual);
            UpdateAnimations(explicitAnimationCollection, visual);
        }

        private static void UpdateAnimations(AvaloniaList<CompositionAnimation>? collections, Visual visual)
        {
            if (collections is ImplicitAnimationCollection implicitCollection)
            {
                if (ElementComposition.GetElementVisual(visual) is { } compositionVisual)
                {
                    compositionVisual.ImplicitAnimations =
                        GetEnableAnimations(visual) ? implicitCollection.GetAnimations(visual) : null;
                }
            }
            else if (collections is ExplicitAnimationCollection explicitCollection)
            {
                if (ElementComposition.GetElementVisual(visual) is { } compositionVisual && GetEnableAnimations(visual))
                {
                    explicitCollection.Attach(visual);
                }
            }
        }
    }
}
