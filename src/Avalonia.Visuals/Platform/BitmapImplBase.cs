using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Avalonia.Platform
{
    public abstract class BitmapImplBase : IBitmapImpl
    {
        private volatile int refCount = 1;

        public void AddReference()
        {
            System.Threading.Interlocked.Increment(ref refCount);
        }

        public void Dispose()
        {
            var current = refCount;
            while (true)
            {
                var old = System.Threading.Interlocked.CompareExchange(ref refCount, current - 1, current);
                if (current == old)
                {
                    if (old == 1)
                    {
                        DisposeCore();
                    }
                    return;
                }
                else
                {
                    current = old;
                }
            }
        }
        public abstract int PixelWidth { get; }
        public abstract int PixelHeight { get; }

        public abstract void Save(string fileName);
        public abstract void Save(Stream stream);
        protected abstract void DisposeCore();
    }
}
