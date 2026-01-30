using System;
using System.Linq;
using System.Numerics;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public abstract class CompositionAnimation : AvaloniaObject, ICompositionTransition, ICompositionAnimation
    {
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

        public event EventHandler? AnimationInvalidated;

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

        private CompositionKeyFrameInstance[]? _keyFramesInstances;

        IDisposable ICompositionAnimation.Apply(Visual parent, IObservable<bool> match)
        {
            // Terrible terrible terrible code ahead
            // Just a proof of concept, trying to make it work
            
            var disposable = new CompositeDisposable();

            var shouldContinue = false;
            var subject = new LightweightSubject<bool>();
            var count = Children.Count;
            var instances = new CompositionKeyFrameInstance[count];
            var hasInitialValue = new bool[count];
            for (var i = 0; i < count; i++)
            {
                var index = i;
                instances[index] = Children[index].Instance(parent);
                disposable.Add(instances[index]);
                hasInitialValue[index] = instances[index].IsSet(CompositionKeyFrameInstance.InstanceValueProperty);
                instances[index].PropertyChanged += InstancePropChanged;
                disposable.Add(Disposable.Create(() =>
                {
                    instances[index].PropertyChanged -= InstancePropChanged;
                }));

                void InstancePropChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
                {
                    if (e.Property == CompositionKeyFrameInstance.InstanceValueProperty)
                    {
                        hasInitialValue[index] = true;
                        subject.OnNext(true);
                    }
                }
            }
            _keyFramesInstances = instances;

            disposable.Add(match.Subscribe(value =>
            {
                shouldContinue = value;
                subject.OnNext(true);
            }));
            
            PropertyChanged += ThisOnPropertyChanged;
            disposable.Add(Disposable.Create(() =>
            {
                PropertyChanged -= ThisOnPropertyChanged;
            }));

            void ThisOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == IsEnabledProperty)
                {
                    subject.OnNext(true);
                }
            }

            disposable.Add(subject
                .Where(_ => shouldContinue && IsEnabled && hasInitialValue.All(b => b))
                .Subscribe(_ =>
                {
                    _keyFramesInstances = instances;
                    if (GetCompositionAnimation(parent) is KeyFrameAnimation { Target: not null } newAnimation)
                    {
                        Attach(parent, newAnimation);
                    }
                }));

            return Disposable.Create(() =>
            {
                if (_keyFramesInstances == instances)
                {
                    _keyFramesInstances = null;
                }

                disposable.Dispose();
                Detach();
            });
        }

        Rendering.Composition.Animations.CompositionAnimation? ICompositionTransition.GetCompositionAnimation(Visual parent)
        {
            return !IsEnabled ? null : GetCompositionAnimation(parent);
        }

        protected abstract Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent);

        protected virtual void SetAnimationValues(Rendering.Composition.Animations.CompositionAnimation animation)
        {
            if (animation is KeyFrameAnimation keyFrameAnimation)
            {
                keyFrameAnimation.Duration = Duration;
                keyFrameAnimation.DelayTime = Delay;
                keyFrameAnimation.IterationBehavior = IterationBehavior;
                keyFrameAnimation.IterationCount = IterationCount;
                keyFrameAnimation.StopBehavior = StopBehavior;

                if (_keyFramesInstances is not null)
                {
                    foreach (var instanceKeyFrame in _keyFramesInstances)
                    {
                        SetKeyFrame(keyFrameAnimation, instanceKeyFrame.KeyFrame, instanceKeyFrame.Value);
                    }
                }
                else if (Children.Any())
                {
                    foreach (var frame in Children)
                    {
                        SetKeyFrame(keyFrameAnimation, frame, frame.Value);
                    }
                }
                else
                {
                    keyFrameAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                }
            }
        }

        private static void SetKeyFrame(KeyFrameAnimation animation, CompositionKeyFrame frame, object? value)
        {
            var easing = (frame.Easing ?? animation.Compositor.DefaultEasing) as Easing;

            if(frame is ExpressionKeyFrame && value is string str)
            {
                animation.InsertExpressionKeyFrame(frame.NormalizedProgressKey, str, easing);
            }
            else if(animation is VectorKeyFrameAnimation vectorKeyFrameAnimation && value is Vector vector)
            {
                vectorKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector, easing!);
            }
            else if (animation is Vector2KeyFrameAnimation vector2KeyFrameAnimation && value is Vector2 vector2)
            {
                vector2KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector2, easing!);
            }
            else if (animation is Vector3KeyFrameAnimation vector3KeyFrameAnimation && value is Vector3 vector3)
            {
                vector3KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector3, easing!);
            }
            else if (animation is Vector3KeyFrameAnimation vector3KeyFrameAnimation1 && value is double vector3X)
            {
                vector3KeyFrameAnimation1.InsertKeyFrame(frame.NormalizedProgressKey, new Vector3((float)vector3X, 0, 0), easing!);
            }
            else if (animation is Vector4KeyFrameAnimation vector4KeyFrameAnimation && value is Vector4 vector4)
            {
                vector4KeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, vector4, easing!);
            }
            else if (animation is ScalarKeyFrameAnimation scalarKeyFrameAnimation && value is float scalar)
            {
                scalarKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, scalar, easing!);
            }
            else if (animation is QuaternionKeyFrameAnimation quaternionKeyFrameAnimation && value is Quaternion quaternion)
            {
                quaternionKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, quaternion, easing!);
            }
            else if (animation is BooleanKeyFrameAnimation booleanKeyFrameAnimation && value is bool boolean)
            {
                booleanKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, boolean, easing!);
            }
            else if (animation is DoubleKeyFrameAnimation doubleKeyFrameAnimation && value is double val)
            {
                doubleKeyFrameAnimation.InsertKeyFrame(frame.NormalizedProgressKey, val, easing!);
            }
            else if (animation is ColorKeyFrameAnimation colorKeyFrameAnimation && value is Color color)
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
