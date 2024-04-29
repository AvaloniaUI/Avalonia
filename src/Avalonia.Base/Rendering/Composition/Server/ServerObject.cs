using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side <see cref="CompositionObject" /> counterpart.
    /// Is responsible for animation activation and invalidation
    /// </summary>
    internal abstract class ServerObject : SimpleServerObject, IExpressionObject
    {
        private uint _activationCount;
        private ServerObjectAnimations? _animations;
        public ServerObjectAnimations? Animations => _animations;
        public ServerObjectAnimations GetOrCreateAnimations() => _animations ??= new(this);
        public bool IsActive => _activationCount != 0;
            
        public ServerObject(ServerCompositor compositor) : base(compositor)
        {
        }

        public void Activate()
        {
            _activationCount++;
            if (_activationCount == 1)
                Activated();
        }

        public void Deactivate()
        {
#if DEBUG
            if (_activationCount == 0)
                throw new InvalidOperationException();
#endif
            _activationCount--;
            if (_activationCount == 0)
                Deactivated();
        }

        private void Activated() => _animations?.Activated();

        private void Deactivated() => _animations?.Deactivated();

        protected new void SetValue<T>(CompositionProperty prop, ref T field, T value)
        {
            field = value;
            _animations?.OnSetDirectValue(prop);
        }

        protected void SetAnimatedValue<T>(CompositionProperty<T> prop, ref T field,
            TimeSpan committedAt, IAnimationInstance animation) where T : struct
        {
            GetOrCreateAnimations().OnSetAnimatedValue(prop, ref field, committedAt, animation);
        }

        protected void SetAnimatedValue<T>(CompositionProperty property, out T field, T value)
        {
            field = value;
            _animations?.RemoveAnimationForProperty(property);
        }
        
        public virtual void NotifyAnimatedValueChanged(CompositionProperty prop) => ValuesInvalidated();
        
        public virtual CompositionProperty? GetCompositionProperty(string fieldName) => null;
        ExpressionVariant IExpressionObject.GetProperty(string name)
        {
            if (_animations == null)
                return CompositionProperty.Find(this.GetType(), name)?.GetVariant?.Invoke(this) ?? default;

            return _animations.GetPropertyForAnimation(name);
        }
    }
}
