using System;

namespace Avalonia.Data.Core
{
    public readonly struct BindingValue<T>
    {
        private readonly T _value;

        public BindingValue(T value)
        {
            _value = value;
            HasValue = true;
            Error = null;
        }

        public BindingValue(Exception error, FallbackValue<T> fallbackValue)
        {
            _value = fallbackValue.HasValue ? fallbackValue.Value : default;
            HasValue = fallbackValue.HasValue;
            Error = error;
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

        public Exception Error { get; }
        
        public override string ToString() => Error?.Message.ToString() ?? Value?.ToString();
    }
}
