using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    /// <summary>
    /// Represents a transition that animates a composition property of a visual.
    /// </summary>
    /// <typeparam name="T">The type of the composition property value.</typeparam>
    public abstract class CompositionTransition<T> : CompositionTransitionBase
    {
        static CompositionTransition()
        {
            PropertyProperty.Changed.AddClassHandler<CompositionTransition<T>>((x, e) => x.OnTargetChanged(e));
        }

        private void OnTargetChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.NewValue is AvaloniaProperty newValue))
            {
                if (newValue.OwnerType != typeof(Composition))
                    throw new ArgumentException
                        ($"Invalid Target property \"{newValue.Name}\" for this animation: {GetType().Name}. Target must be a Composition property.");

                if (!newValue.PropertyType.IsAssignableFrom(typeof(T)))
                    throw new InvalidCastException
                        ($"Invalid Composition Target \"{newValue.Name}\" for this animation: {GetType().Name}. Expecting Target type \"{typeof(T).Name}\", got \"{newValue.PropertyType.Name}\".");
            }
        }

        /// <inheritdoc/>
        protected override void SetAnimationValues(CompositionAnimation animation)
        {
            base.SetAnimationValues(animation);

            animation.Target = Property?.Name;
        }
    }

    /// <summary>
    /// Provides the base functionality for composition transitions.
    /// </summary>
    public abstract class CompositionTransitionBase : AvaloniaObject, ICompositionTransition
    {
        private protected AvaloniaProperty? _property;
        private Easing _easing = new SplineEasing(new KeySpline(0.25, 0.1, 0.25, 1.0));

        /// <summary>
        /// Defines the <see cref="IterationCount"/> property.
        /// </summary>
        public static readonly StyledProperty<int> IterationCountProperty = AvaloniaProperty.Register<CompositionTransitionBase, int>(
            nameof(IterationCount), defaultValue: 1);

        /// <summary>
        /// Defines the <see cref="Duration"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<CompositionTransitionBase, TimeSpan>(
            nameof(Duration), defaultValue: TimeSpan.Zero);

        /// <summary>
        /// Defines the <see cref="Delay"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<CompositionTransitionBase, TimeSpan>(
            nameof(Delay), defaultValue: TimeSpan.Zero);

        /// <summary>
        /// Defines the <see cref="IterationBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<AnimationIterationBehavior> IterationBehaviorProperty = AvaloniaProperty.Register<CompositionTransitionBase, AnimationIterationBehavior>(
            nameof(IterationBehavior), defaultValue: AnimationIterationBehavior.Count);

        /// <summary>
        /// Defines the <see cref="StopBehavior"/> property.
        /// </summary>
        public static readonly StyledProperty<AnimationStopBehavior> StopBehaviorProperty = AvaloniaProperty.Register<CompositionTransitionBase, AnimationStopBehavior>(
            nameof(StopBehavior), defaultValue: AnimationStopBehavior.LeaveCurrentValue);

        /// <summary>
        /// Defines the <see cref="Easing"/> property.
        /// </summary>
        public static readonly DirectProperty<CompositionTransitionBase, Easing> EasingProperty =
            AvaloniaProperty.RegisterDirect<CompositionTransitionBase, Easing>(
                nameof(Easing),
                o => o._easing,
                (o, v) => o._easing = v);

        /// <summary>
        /// Gets or sets the composition property targeted by the transition.
        /// </summary>
        [DisallowNull]
        public AvaloniaProperty? Property
        {
            get { return _property; }
            set { SetAndRaise(PropertyProperty, ref _property, value); }
        }

        /// <summary>
        /// Gets or sets the easing function used by the transition.
        /// </summary>
        public Easing Easing
        {
            get { return _easing; }
            set { SetAndRaise(EasingProperty, ref _easing, value); }
        }

        /// <summary>
        /// Defines the <see cref="Property"/> property.
        /// </summary>
        public static readonly DirectProperty<CompositionTransitionBase, AvaloniaProperty?> PropertyProperty =
            AvaloniaProperty.RegisterDirect<CompositionTransitionBase, AvaloniaProperty?>(
                nameof(Property),
                o => o._property,
                (o, v) => o._property = v);

        private Visual? _attachedVisual;
        private KeyFrameAnimation? _animation;

        /// <summary>
        /// Occurs when the transition's animation needs to be recreated.
        /// </summary>
        public event EventHandler? AnimationInvalidated;

        /// <summary>
        /// Gets or sets the number of times the animation repeats.
        /// </summary>
        public int IterationCount
        {
            get => GetValue(IterationCountProperty);
            set => SetValue(IterationCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of each animation iteration.
        /// </summary>
        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the delay before the animation starts.
        /// </summary>
        public TimeSpan Delay
        {
            get => GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the behavior used when the animation reaches the end of an iteration.
        /// </summary>
        public AnimationIterationBehavior IterationBehavior
        {
            get => GetValue(IterationBehaviorProperty);
            set => SetValue(IterationBehaviorProperty, value);
        }

        /// <summary>
        /// Gets or sets the behavior used when the animation stops.
        /// </summary>
        public AnimationStopBehavior StopBehavior
        {
            get => GetValue(StopBehaviorProperty);
            set => SetValue(StopBehaviorProperty, value);
        }

        CompositionAnimation? ICompositionTransition.GetCompositionAnimation(Visual parent)
        {
            return GetCompositionAnimation(parent);
        }

        /// <summary>
        /// Creates the composition animation for the specified visual.
        /// </summary>
        /// <param name="parent">The visual to which the animation will be applied.</param>
        /// <returns>The composition animation, or <see langword="null"/> if no animation can be created.</returns>
        protected abstract CompositionAnimation? GetCompositionAnimation(Visual parent);

        /// <summary>
        /// Applies the transition settings to a composition animation.
        /// </summary>
        /// <param name="animation">The composition animation to configure.</param>
        protected virtual void SetAnimationValues(CompositionAnimation animation)
        {
            if (animation is KeyFrameAnimation keyFrameAnimation)
            {
                keyFrameAnimation.Duration = Duration;
                keyFrameAnimation.DelayTime = Delay;
                keyFrameAnimation.IterationBehavior = IterationBehavior;
                keyFrameAnimation.IterationCount = IterationCount;
                keyFrameAnimation.StopBehavior = StopBehavior;
                keyFrameAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", Easing);
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

            if (_attachedVisual != null && _animation?.Target is { } newTarget && ElementComposition.GetElementVisual(_attachedVisual) is { } newCompositionVisual)
            {
                newCompositionVisual.StartAnimation(newTarget, _animation);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DurationProperty || change.Property == DelayProperty)
                RaiseAnimationInvalidated();
        }

        /// <summary>
        /// Raises the <see cref="AnimationInvalidated"/> event.
        /// </summary>
        protected void RaiseAnimationInvalidated()
        {
            AnimationInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }
}
