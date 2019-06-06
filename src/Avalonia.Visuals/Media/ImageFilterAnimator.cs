using System;
using System.Reactive.Disposables;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Collections;

namespace Avalonia.Media
{
    class ImageFilterAnimator : AvaloniaList<AnimatorKeyFrame>, IAnimator
    {
        private IAnimator _inner;
        public AvaloniaProperty Property { get; set; }

        public IDisposable Apply(Animation.Animation animation, Animatable control,
            IClock clock, IObservable<bool> match, Action onComplete)
        {
            var filter = control.GetValue(ImageFilter.ImageFilterProperty);
            if (filter == null)
                control.SetValue(ImageFilter.ImageFilterProperty, filter = new DropShadowImageFilter());

            if (!(filter is DropShadowImageFilter dsf))
                return Disposable.Empty;

            if (_inner == null)
            {
                if (Property.PropertyType == typeof(Vector))
                    _inner = new VectorAnimator();
                else if (Property.PropertyType == typeof(Color))
                    _inner = new ColorAnimator();
                else if (Property.PropertyType == typeof(double))
                    _inner = new DoubleAnimator();
                else
                    return Disposable.Empty;
                foreach (AnimatorKeyFrame keyframe in this)
                    _inner.Add(keyframe);
                _inner.Property = Property;
            }
            
            return _inner.Apply(animation, dsf, clock ?? control.Clock, match, onComplete);
        }
    }
}
