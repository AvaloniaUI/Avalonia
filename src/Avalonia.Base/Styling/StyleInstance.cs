using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    /// <summary>
    /// Stores state for a <see cref="Style"/> that has been instanced on a control.
    /// </summary>
    /// <remarks>
    /// <see cref="StyleInstance"/> is based on <see cref="ValueFrame"/> meaning that it is 
    /// injected directly into the value store of an <see cref="AvaloniaObject"/>. Depending on
    /// the setters present on the style, it may be possible to share a single style instance
    /// among all controls that the style is applied to, meaning that a single style instance can
    /// apply to multiple controls.
    /// </remarks>
    internal class StyleInstance : ValueFrame, IStyleInstance, IStyleActivatorSink, IDisposable
    {
        private readonly IStyleActivator? _activator;
        private bool _isActive;
        private List<ISetterInstance>? _setters;
        private List<IAnimation>? _animations;
        private LightweightSubject<bool>? _animationTrigger;

        public StyleInstance(
            IStyle style,
            IStyleActivator? activator,
            FrameType type)
            : base(GetPriority(activator), type)
        {
            _activator = activator;
            Source = style;
        }

        public bool HasActivator => _activator is object;

        public IStyle Source { get; }

        bool IStyleInstance.IsActive => _isActive;
        
        public void Add(ISetterInstance instance)
        {
            if (instance is IValueEntry valueEntry)
            {
                if (Contains(valueEntry.Property))
                    throw new InvalidOperationException(
                        $"Duplicate setter encountered for property '{valueEntry.Property}' in '{Source}'.");
                Add(valueEntry);
            }
            else
                (_setters ??= new()).Add(instance);
        }

        public void Add(IList<IAnimation> animations)
        {
            if (_animations is null)
                _animations = new List<IAnimation>(animations);
            else
                _animations.AddRange(animations);
        }

        public void ApplyAnimations(AvaloniaObject control)
        {
            if (_animations is not null && control is Animatable animatable)
            {
                _animationTrigger ??= new LightweightSubject<bool>();
                foreach (var animation in _animations)
                    animation.Apply(animatable, null, _animationTrigger);

                if (_activator is null)
                    _animationTrigger.OnNext(true);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _activator?.Dispose();
        }

        public new void MakeShared() => base.MakeShared();

        void IStyleActivatorSink.OnNext(bool value)
        {
            Owner?.OnFrameActivationChanged(this);
            _animationTrigger?.OnNext(value);
        }

        protected override bool GetIsActive(out bool hasChanged)
        {
            var previous = _isActive;

            if (_activator?.IsSubscribed == false)
            {
                _activator.Subscribe(this);
                _animationTrigger?.OnNext(_activator.GetIsActive());
            }

            _isActive = _activator?.GetIsActive() ?? true;
            hasChanged = _isActive != previous;
            return _isActive;
        }

        private static BindingPriority GetPriority(IStyleActivator? activator)
        {
            return activator is not null ? BindingPriority.StyleTrigger : BindingPriority.Style;
        }
    }
}
