using System;
using Avalonia.Rendering.Composition;

namespace Avalonia.Animation
{
    public static class CompositionAnimation
    {
        public static readonly AttachedProperty<ImplicitAnimationCollection?> ImplictionAnimationsProperty =
            AvaloniaProperty.RegisterAttached<Visual, ImplicitAnimationCollection?>(
                "ImplictionAnimations",
                typeof(CompositionAnimation));

        public static void SetImplictionAnimations(Visual visual, ImplicitAnimationCollection? value)
        {
            visual?.SetValue(ImplictionAnimationsProperty, value);
        }

        public static ImplicitAnimationCollection? GetImplictionAnimations(Visual visual) => visual.GetValue(ImplictionAnimationsProperty);

        public static readonly AttachedProperty<bool> EnableAnimationsProperty =
            AvaloniaProperty.RegisterAttached<Visual, bool>(
                "EnableAnimations",
                typeof(CompositionAnimation), defaultValue: true, inherits: true);

        public static void SetEnableAnimations(Visual visual, bool value)
        {
            visual?.SetValue(EnableAnimationsProperty, value);
        }

        public static bool GetEnableAnimations(Visual visual) => visual.GetValue(EnableAnimationsProperty);

        static CompositionAnimation()
        {
            ImplictionAnimationsProperty.Changed.AddClassHandler<Visual>(OnAnimationsPropertyChanged);
            EnableAnimationsProperty.Changed.AddClassHandler<Visual>(OnAnimationsPropertyChanged);
        }

        private static void OnAnimationsPropertyChanged(Visual visual, AvaloniaPropertyChangedEventArgs args)
        {
            void UpdateAnimations(object? sender, EventArgs? e)
            {
                if (sender is ImplicitAnimationCollection collection)
                {
                    if (ElementComposition.GetElementVisual(visual) is { } compositionVisual)
                    {
                        compositionVisual.ImplicitAnimations =
                            GetEnableAnimations(visual) ? collection.GetAnimations(visual) : null;
                    }
                }
            }

            void AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
            {
                if (args.NewValue is ImplicitAnimationCollection ImplicitAnimationCollection)
                    UpdateAnimations(ImplicitAnimationCollection, EventArgs.Empty);
            }

            if (args.Property == ImplictionAnimationsProperty)
            {
                if (args.OldValue is ImplicitAnimationCollection oldSet)
                {
                    oldSet.Invalidated -= UpdateAnimations;
                    visual.AttachedToVisualTree -= AttachedToVisualTree;
                }


                if (args.NewValue is ImplicitAnimationCollection newSet)
                {
                    newSet.Invalidated += UpdateAnimations;
                    UpdateAnimations(newSet, null);
                    visual.AttachedToVisualTree += AttachedToVisualTree;
                }
            }
            else if (args.Property == EnableAnimationsProperty)
            {
                UpdateAnimations(GetImplictionAnimations(visual), null);
            }
        }
    }
}
