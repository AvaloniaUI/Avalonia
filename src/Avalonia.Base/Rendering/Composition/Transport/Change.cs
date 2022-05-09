using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Rendering.Composition.Transport
{
    struct Change<T>
    {
        private T? _value;

        public bool IsSet { get; private set; }

        public T? Value
        {
            get
            {
                if(!IsSet)
                    throw new InvalidOperationException();
                return _value;
            }
            set
            {
                IsSet = true;
                _value = value;
            }
        }

        public void Reset()
        {
            _value = default;
            IsSet = false;
        }
    }

    struct AnimatedChange<T>
    {
        private T? _value;
        private IAnimationInstance? _animation;

        public bool IsValue { get; private set; }
        public bool IsAnimation { get; private set; }

        public T Value
        {
            get
            {
                if(!IsValue)
                    throw new InvalidOperationException();
                return _value!;
            }
            set
            {
                IsAnimation = false;
                _animation = null;
                IsValue = true;
                _value = value;
            }
        }
        
        public IAnimationInstance Animation
        {
            get
            {
                if(!IsAnimation)
                    throw new InvalidOperationException();
                return _animation!;
            }
            set
            {
                IsValue = false;
                _value = default;
                IsAnimation = true;
                _animation = value;
            }
        }

        public void Reset()
        {
            this = default;
        }
    }
}