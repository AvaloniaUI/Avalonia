using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public abstract class ImageEffect : Animatable, IMutableImageEffect
    {
        private WeakList<Visual> _subscribers = new WeakList<Visual>();
        public abstract IImageEffect ToImmutable();

        public static StyledProperty<IImageEffect> FillImageEffectProperty
            = AvaloniaProperty.RegisterAttached<ImageEffect, Visual, IImageEffect>("ImageEffect");
        
        static ImageEffect()
        {
            Animation.Animation.RegisterAnimator<ImageEffectAnimator>(p =>
                p.OwnerType == typeof(DropShadowImageEffect), 100);
        }
        
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : ImageEffect
        {
            void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                if(e.Sender is ImageEffect filter)
                    foreach (var s in filter._subscribers)
                        s.InvalidateVisual();
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Invalidate);
            }
        }

        public static void InitializeProperty(AvaloniaProperty<IImageEffect> property)
        {
            property.Changed.Subscribe(e =>
            {
                if (e.Sender is Visual v)
                {
                    if(e.OldValue is ImageEffect oldFilter)
                        oldFilter._subscribers.Remove(v);
                    if (e.NewValue is ImageEffect newFilter) 
                        newFilter._subscribers.Add(v);
                }
            });
        }

        public static bool Equals(IImageEffect left, IImageEffect right)
        {
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (left == right)
                return true;
            return left?.Equals(right) == true;
        }
    }
}
