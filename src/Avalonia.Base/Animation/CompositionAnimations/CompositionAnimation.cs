using System;
using System.Linq;
using System.Numerics;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public abstract class CompositionAnimation : AvaloniaObject
    {
        internal event EventHandler? AnimationInvalidated;

        public static readonly StyledProperty<bool> IsEnabledProperty = AvaloniaProperty.Register<CompositionAnimation, bool>(
            nameof(IsEnabled), defaultValue: true);

        public static readonly StyledProperty<int> IterationCountProperty = AvaloniaProperty.Register<CompositionAnimation, int>(
            nameof(IterationCount), defaultValue: 1);

        public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<CompositionAnimation, TimeSpan>(
            nameof(Duration), defaultValue: TimeSpan.Zero);

        public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<CompositionAnimation, TimeSpan>(
            nameof(Delay), defaultValue: TimeSpan.Zero);

        public static readonly StyledProperty<AnimationIterationBehavior> IterationBehaviorProperty = AvaloniaProperty.Register<CompositionAnimation, AnimationIterationBehavior>(
            nameof(IterationBehavior), defaultValue: AnimationIterationBehavior.Count);

        public static readonly StyledProperty<AnimationStopBehavior> StopBehaviorProperty = AvaloniaProperty.Register<CompositionAnimation, AnimationStopBehavior>(
            nameof(StopBehavior), defaultValue: AnimationStopBehavior.LeaveCurrentValue);
        
        private Visual? _attachedVisual;
        private KeyFrameAnimation? _animation;

        [Content]
        public AvaloniaList<CompositionKeyFrame> Children { get; } = new();

        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        public int IterationCount
        {
            get => GetValue(IterationCountProperty);
            set => SetValue(IterationCountProperty, value);
        }

        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public TimeSpan Delay
        {
            get => GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public AnimationIterationBehavior IterationBehavior
        {
            get => GetValue(IterationBehaviorProperty);
            set => SetValue(IterationBehaviorProperty, value);
        }

        public AnimationStopBehavior StopBehavior
        {
            get => GetValue(StopBehaviorProperty);
            set => SetValue(StopBehaviorProperty, value);
        }

        internal Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimationInternal(Visual parent)
        {
            return !IsEnabled ? null : GetCompositionAnimation(parent);
        }

        public abstract Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent);

        protected virtual void SetAnimationValues(Rendering.Composition.Animations.CompositionAnimation animation)
        {
            if (animation is KeyFrameAnimation keyFrameAnimation)
            {
                keyFrameAnimation.Duration = Duration;
                keyFrameAnimation.DelayTime = Delay;
                keyFrameAnimation.IterationBehavior = IterationBehavior;
                keyFrameAnimation.IterationCount = IterationCount;
                keyFrameAnimation.StopBehavior = StopBehavior;

                if (Children.Any())
                    foreach (var frame in Children)
                    {
                        SetKeyFrame(keyFrameAnimation, frame);
                    }
                else
                {
                    keyFrameAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                }
            }
        }

        private static void SetKeyFrame(KeyFrameAnimation animation, CompositionKeyFrame frame)
        {
            var easing = (frame.Easing ?? animation.Compositor.DefaultEasing) as Easing;

            if(frame is ExpressionKeyFrame expressionKeyFrame && expressionKeyFrame.Value is string value)
            {
                animation.InsertExpressionKeyFrame(frame.NormalizedProgressKey, value, easing);
            }
            else if(animation is VectorKeyFrameAnimation vectorKeyFrameAnimation && frame.Value is Vector vector)
            {
                vectorKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector, easing!);
            }
            else if (animation is Vector2KeyFrameAnimation vector2KeyFrameAnimation && frame.Value is Vector2 vector2)
            {
                vector2KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector2, easing!);
            }
            else if (animation is Vector3KeyFrameAnimation vector3KeyFrameAnimation && frame.Value is Vector3 vector3)
            {
                vector3KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector3, easing!);
            }
            else if (animation is Vector4KeyFrameAnimation vector4KeyFrameAnimation && frame.Value is Vector4 vector4)
            {
                vector4KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector4, easing!);
            }
            else if (animation is ScalarKeyFrameAnimation scalarKeyFrameAnimation && frame.Value is float scalar)
            {
                scalarKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, scalar, easing!);
            }
            else if (animation is QuaternionKeyFrameAnimation quaternionKeyFrameAnimation && frame.Value is Quaternion quaternion)
            {
                quaternionKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, quaternion, easing!);
            }
            else if (animation is BooleanKeyFrameAnimation booleanKeyFrameAnimation && frame.Value is bool boolean)
            {
                booleanKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, boolean, easing!);
            }
            else if (animation is DoubleKeyFrameAnimation doubleKeyFrameAnimation && frame.Value is double val)
            {
                doubleKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, val, easing!);
            }
            else if (animation is ColorKeyFrameAnimation colorKeyFrameAnimation && frame.Value is Color color)
            {
                colorKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, color, easing!);
            }
        }

        internal void Detach()
        {
            if (_attachedVisual != null && _animation?.Target is { } oldTarget && ElementComposition.GetElementVisual(_attachedVisual) is { } oldCompositionVisual)
            {
                oldCompositionVisual.StopAnimation(oldTarget);
            }

            _animation = null;
            _attachedVisual = null;
        }

        internal void Attach(Visual? visual, KeyFrameAnimation? animation)
        {
            Detach();

            _attachedVisual = visual;
            _animation = animation;

            if (IsEnabled && _attachedVisual != null && _animation?.Target is { } newTarget && ElementComposition.GetElementVisual(_attachedVisual) is { } newCompositionVisual)
            {
                newCompositionVisual.StartAnimation(newTarget, _animation);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DurationProperty || change.Property == DelayProperty)
                RaiseAnimationInvalidated();
            else if (change.Property == IsEnabledProperty)
                OnEnabledChanged();
        }

        private void OnEnabledChanged()
        {
            if (_attachedVisual != null && _animation?.Target is { } target && ElementComposition.GetElementVisual(_attachedVisual) is { } compositionVisual)
            {
                if (IsEnabled)
                    compositionVisual.StartAnimation(target, _animation);
                else
                    compositionVisual.StopAnimation(target);
            }
            else
                RaiseAnimationInvalidated();
        }

        protected void RaiseAnimationInvalidated()
        {
            AnimationInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}
