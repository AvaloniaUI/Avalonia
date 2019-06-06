using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public abstract class ImageFilter : Animatable, IMutableImageFilter
    {
        private WeakList<Visual> _subscribers = new WeakList<Visual>();
        public abstract IImageFilter ToImmutable();

        public static StyledProperty<IImageFilter> ImageFilterProperty
            = AvaloniaProperty.RegisterAttached<ImageFilter, Visual, IImageFilter>("ImageFilter");
        
        static ImageFilter()
        {
            Animation.Animation.RegisterAnimator<ImageFilterAnimator>(p =>
                p.OwnerType == typeof(DropShadowImageFilter), 100);
        }
        
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : ImageFilter
        {
            void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                if(e.Sender is ImageFilter filter)
                    foreach (var s in filter._subscribers)
                        s.InvalidateVisual();
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Invalidate);
            }
        }

        public static void InitializeProperty(AvaloniaProperty<IImageFilter> property)
        {
            property.Changed.Subscribe(e =>
            {
                if (e.Sender is Visual v)
                {
                    if(e.OldValue is ImageFilter oldFilter)
                        oldFilter._subscribers.Remove(v);
                    if (e.NewValue is ImageFilter newFilter) 
                        newFilter._subscribers.Add(v);
                }
            });
        }

        public static bool Equals(IImageFilter left, IImageFilter right)
        {
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (left == right)
                return true;
            return left?.Equals(right) == true;
        }
    }
}
