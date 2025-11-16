using System;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public abstract class KeyFrameImplicitAnimation : ImplicitAnimation
    {
        public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<KeyFrameImplicitAnimation, TimeSpan>(
            nameof(Duration), defaultValue: TimeSpan.Zero);

        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<KeyFrameImplicitAnimation, TimeSpan>(
            nameof(Delay), defaultValue: TimeSpan.Zero);

        public TimeSpan Delay
        {
            get => GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        [Content]
        public AvaloniaList<ExpressionKeyFrame> Children { get; } = new();

        protected override void SetBaseValues(Rendering.Composition.Animations.CompositionAnimation animation)
        {
            base.SetBaseValues(animation);

            if (animation is KeyFrameAnimation keyFrameAnimation)
            {
                keyFrameAnimation.Duration = Duration;
                keyFrameAnimation.DelayTime = Delay;

                foreach (var frame in Children)
                {
                    if (frame.Value is { } value)
                        keyFrameAnimation.InsertExpressionKeyFrame(frame.NormalizedProgressKey, value, frame.Easing as Easings.Easing);
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if(change.Property == DurationProperty || change.Property == DelayProperty)
                RaiseAnimationInvalidated();
        }
    }

    public abstract class ImplicitAnimation : AvaloniaObject
    {
        internal event EventHandler? AnimationInvalidated;
        
        protected internal abstract string? Property { get; }
        
        public static readonly StyledProperty<bool> IsEnabledProperty = AvaloniaProperty.Register<ImplicitAnimation, bool>(
            nameof(IsEnabled), defaultValue: true);

        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        internal Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimationInternal(Visual parent)
        {
            return !IsEnabled ? null : GetCompositionAnimation(parent);
        }

        public abstract Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent);

        protected virtual void SetBaseValues(Rendering.Composition.Animations.CompositionAnimation animation)
        {
            animation.Target = Property;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if(change.Property == IsEnabledProperty)
                RaiseAnimationInvalidated();
        }

        protected void RaiseAnimationInvalidated()
        {
            AnimationInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}
