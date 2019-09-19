using System;

namespace Avalonia.Data.Core
{
    public struct FallbackValue<T>
    {
        private readonly T _value;

        public FallbackValue(T value)
        {
            _value = value;
            HasValue = true;
        }

        public bool HasValue { get; }

        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("BindingValue has no value.");
                }

                return _value;
            }
        }

        public override string ToString() => HasValue ? Value.ToString() : "(unset)";

        public static implicit operator FallbackValue<T>(T value) => new FallbackValue<T>(value);
    }
}
