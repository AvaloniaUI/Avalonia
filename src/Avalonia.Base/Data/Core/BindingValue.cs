using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Data.Core
{
    public readonly struct BindingValue<T>
    {
        public BindingValue(T value)
        {
            Value = value;
            Error = null;
        }

        public BindingValue(Exception error)
        {
            Value = default;
            Error = error;
        }

        public T Value { get; }

        public Exception Error { get; }

        public override string ToString() => Error?.Message.ToString() ?? Value?.ToString();
    }
}
