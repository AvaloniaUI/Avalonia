using System;

namespace Avalonia.Direct2D1
{
    public class Disposable<T> : IDisposable where T : IDisposable
    {
        private readonly IDisposable _extra;

        public Disposable(T inner)
        {
            Inner = inner;
        }

        public Disposable(T inner, IDisposable extra)
        {
            Inner = inner;
            _extra = extra;
        }

        public T Inner { get; }

        public static implicit operator T(Disposable<T> i)
        {
            return i.Inner;
        }

        public void Dispose()
        {
            Inner.Dispose();
            _extra?.Dispose();
        }
    }
}
