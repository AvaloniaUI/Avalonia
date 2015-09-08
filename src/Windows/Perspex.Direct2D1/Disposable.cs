





namespace Perspex.Direct2D1
{
    using System;

    public class Disposable<T> : IDisposable where T : IDisposable
    {
        private IDisposable extra;

        public Disposable(T inner)
        {
            this.Inner = inner;
        }

        public Disposable(T inner, IDisposable extra)
        {
            this.Inner = inner;
            this.extra = extra;
        }

        public T Inner { get; }

        public static implicit operator T(Disposable<T> i)
        {
            return i.Inner;
        }

        public void Dispose()
        {
            this.Inner.Dispose();
            this.extra?.Dispose();
        }
    }
}
