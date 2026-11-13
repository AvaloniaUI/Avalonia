using System;

namespace Avalonia.Android
{
    internal struct DisposableClosable<T>(T? instance) : IDisposable where T : Java.IO.ICloseable
    {
        public T? Instance { get; } = instance;

        public void Dispose()
        {
            Instance?.Close();
            if (Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
