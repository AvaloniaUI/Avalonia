using System;

namespace Avalonia.Direct2D1
{
    internal readonly record struct OptionalDispose<T> : IDisposable where T : IDisposable
    {
        private readonly bool _dispose;

        public OptionalDispose(T value, bool dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public void Dispose()
        {
            if (_dispose) Value?.Dispose();
        }
    }
}
